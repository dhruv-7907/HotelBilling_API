USE [HotelBillingDB];
GO

-- ================================================================
-- 002_PerformanceAndSafetyPatch.sql
-- Incremental patch: indexing, constraints, and procedure hardening
-- ================================================================

-- -----------------------------
-- 1) Indexes for hot paths
-- -----------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_RoomDatesStatus' AND object_id = OBJECT_ID('dbo.Reservations'))
    CREATE NONCLUSTERED INDEX IX_Reservations_RoomDatesStatus
    ON dbo.Reservations(RoomId, CheckIn, CheckOut, Status)
    WHERE IsDeleted = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_StatusCreatedAt' AND object_id = OBJECT_ID('dbo.Reservations'))
    CREATE NONCLUSTERED INDEX IX_Reservations_StatusCreatedAt
    ON dbo.Reservations(Status, CreatedAt)
    WHERE IsDeleted = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Invoices_StatusInvoiceDate' AND object_id = OBJECT_ID('dbo.Invoices'))
    CREATE NONCLUSTERED INDEX IX_Invoices_StatusInvoiceDate
    ON dbo.Invoices(Status, InvoiceDate)
    WHERE IsDeleted = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Invoices_ReservationId' AND object_id = OBJECT_ID('dbo.Invoices'))
    CREATE NONCLUSTERED INDEX IX_Invoices_ReservationId
    ON dbo.Invoices(ReservationId)
    WHERE IsDeleted = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InvoiceLineItems_InvoiceId' AND object_id = OBJECT_ID('dbo.InvoiceLineItems'))
    CREATE NONCLUSTERED INDEX IX_InvoiceLineItems_InvoiceId
    ON dbo.InvoiceLineItems(InvoiceId)
    WHERE IsDeleted = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HousekeepingTasks_RoomStatus' AND object_id = OBJECT_ID('dbo.HousekeepingTasks'))
    CREATE NONCLUSTERED INDEX IX_HousekeepingTasks_RoomStatus
    ON dbo.HousekeepingTasks(RoomId, Status)
    WHERE IsDeleted = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_RefreshToken' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE NONCLUSTERED INDEX IX_Users_RefreshToken
    ON dbo.Users(RefreshToken)
    WHERE IsDeleted = 0 AND RefreshToken IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Guests_Email_NotBlank' AND object_id = OBJECT_ID('dbo.Guests'))
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Guests_Email_NotBlank
    ON dbo.Guests(Email)
    WHERE IsDeleted = 0 AND Email <> '';
GO

-- -----------------------------
-- 2) Financial integrity checks
-- -----------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Invoices_PaidVsTotal'
      AND parent_object_id = OBJECT_ID('dbo.Invoices')
)
ALTER TABLE dbo.Invoices
    ADD CONSTRAINT CK_Invoices_PaidVsTotal
    CHECK (PaidAmount >= 0 AND PaidAmount <= TotalAmount);
GO

-- -----------------------------
-- 3) Room availability fix
-- Exclude NoShow from conflicts
-- -----------------------------
CREATE OR ALTER PROCEDURE [dbo].[sp_CheckRoomAvailability]
    @RoomId     INT,
    @CheckIn    DATE,
    @CheckOut   DATE,
    @ExcludeId  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ConflictCount INT;

    SELECT @ConflictCount = COUNT(*)
    FROM Reservations
    WHERE RoomId    = @RoomId
      AND IsDeleted = 0
      AND Status    NOT IN (4, 5, 6) -- CheckedOut, Cancelled, NoShow
      AND @CheckIn  < CheckOut
      AND @CheckOut > CheckIn
      AND (@ExcludeId IS NULL OR Id <> @ExcludeId);

    SELECT CASE WHEN @ConflictCount = 0 THEN 1 ELSE 0 END AS IsAvailable,
           @ConflictCount AS ConflictingReservations;
END;
GO

-- -----------------------------
-- 4) Night audit hardening
-- Prevent duplicate charge posting
-- -----------------------------
CREATE OR ALTER PROCEDURE [dbo].[sp_NightAudit]
    @AuditDate DATE = NULL,
    @UserId    INT  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @AuditDate IS NULL SET @AuditDate = CAST(GETUTCDATE() AS DATE);

    DECLARE @Posted INT = 0;
    DECLARE @InvoiceNum NVARCHAR(30);

    DECLARE auditCursor CURSOR FOR
        SELECT r.Id, r.GuestId, r.RoomId, r.RatePerNight,
               CASE rm.RoomType WHEN 1 THEN 12.00 ELSE 18.00 END AS GstRate
        FROM Reservations r
        JOIN Rooms rm ON r.RoomId = rm.Id
        WHERE r.Status = 3 AND r.IsDeleted = 0
          AND @AuditDate BETWEEN r.CheckIn AND DATEADD(DAY, -1, r.CheckOut);

    DECLARE @ResId INT, @GuestId INT, @RoomId INT, @Rate DECIMAL(18,2), @GstRate DECIMAL(5,2);
    DECLARE @InvId INT;
    DECLARE @ChargeLabel NVARCHAR(200);

    OPEN auditCursor;
    FETCH NEXT FROM auditCursor INTO @ResId, @GuestId, @RoomId, @Rate, @GstRate;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @ChargeLabel = 'Room charge — ' + CONVERT(NVARCHAR(10), @AuditDate, 23);

        IF NOT EXISTS (
            SELECT 1
            FROM Invoices i
            JOIN InvoiceLineItems li ON li.InvoiceId = i.Id AND li.IsDeleted = 0
            WHERE i.IsDeleted = 0
              AND i.ReservationId = @ResId
              AND li.Description = @ChargeLabel
        )
        BEGIN
            BEGIN TRANSACTION;

            SET @InvoiceNum = 'INV-' + FORMAT(GETUTCDATE(),'yyyyMMddHHmmss') + '-' + CAST(@ResId AS NVARCHAR(20));
            DECLARE @GstAmt DECIMAL(18,2) = ROUND(@Rate * @GstRate / 100, 2);

            INSERT INTO Invoices
            (
                InvoiceNumber, ReservationId, GuestId, DueDate,
                Subtotal, GstAmount, TotalAmount, BalanceDue, Status, Notes, CreatedAt
            )
            VALUES
            (
                @InvoiceNum, @ResId, @GuestId, DATEADD(DAY, 1, @AuditDate),
                @Rate, @GstAmt, @Rate + @GstAmt, @Rate + @GstAmt, 2,
                'Night audit charge — ' + CONVERT(NVARCHAR(10), @AuditDate, 23), GETUTCDATE()
            );

            SET @InvId = SCOPE_IDENTITY();

            INSERT INTO InvoiceLineItems
            (
                InvoiceId, Description, Quantity, UnitPrice, Amount, GstRate, GstAmount, CreatedAt
            )
            VALUES
            (
                @InvId, @ChargeLabel, 1, @Rate, @Rate, @GstRate, @GstAmt, GETUTCDATE()
            );

            COMMIT TRANSACTION;
            SET @Posted = @Posted + 1;
        END

        FETCH NEXT FROM auditCursor INTO @ResId, @GuestId, @RoomId, @Rate, @GstRate;
    END

    CLOSE auditCursor;
    DEALLOCATE auditCursor;

    SELECT @Posted AS NightChargesPosted, @AuditDate AS AuditDate;
END;
GO

