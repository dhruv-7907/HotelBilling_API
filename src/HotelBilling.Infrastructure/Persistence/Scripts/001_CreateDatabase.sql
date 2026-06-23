-- ============================================================
-- Hotel Billing Pro — Database Schema
-- SQL Server 2019 / Azure SQL
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'HotelBillingDB')
    CREATE DATABASE HotelBillingDB;
GO

USE HotelBillingDB;
GO

-- ── Users ──────────────────────────────────────────────────
IF OBJECT_ID('Users') IS NULL
CREATE TABLE Users (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    FullName            NVARCHAR(150)  NOT NULL,
    Email               NVARCHAR(200)  NOT NULL UNIQUE,
    Phone               NVARCHAR(20)   NOT NULL,
    PasswordHash        NVARCHAR(255)  NOT NULL,
    Role                INT            NOT NULL DEFAULT 3,  -- 1=SuperAdmin 2=Admin 3=FrontDesk 4=Housekeeping 5=Accounts
    IsActive            BIT            NOT NULL DEFAULT 1,
    RefreshToken        NVARCHAR(500)  NULL,
    RefreshTokenExpiry  DATETIME2      NULL,
    LastLoginAt         DATETIME2      NULL,
    CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2      NULL,
    IsDeleted           BIT            NOT NULL DEFAULT 0,
    CreatedBy           INT            NULL,
    UpdatedBy           INT            NULL
);
GO

-- ── Guests ─────────────────────────────────────────────────
IF OBJECT_ID('Guests') IS NULL
CREATE TABLE Guests (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(150)  NOT NULL,
    Email           NVARCHAR(200)  NOT NULL DEFAULT '',
    Phone           NVARCHAR(20)   NOT NULL,
    Address         NVARCHAR(500)  NULL,
    City            NVARCHAR(100)  NULL,
    Nationality     NVARCHAR(100)  NULL DEFAULT 'Indian',
    IdType          NVARCHAR(50)   NULL,
    IdNumber        NVARCHAR(100)  NULL,
    DateOfBirth     DATE           NULL,
    IsVip           BIT            NOT NULL DEFAULT 0,
    TotalStays      INT            NOT NULL DEFAULT 0,
    TotalSpent      DECIMAL(18,2)  NOT NULL DEFAULT 0,
    RatingAvg       INT            NOT NULL DEFAULT 0,
    Notes           NVARCHAR(1000) NULL,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2      NULL,
    IsDeleted       BIT            NOT NULL DEFAULT 0,
    CreatedBy       INT            NULL,
    UpdatedBy       INT            NULL
);
GO

-- ── Rooms ──────────────────────────────────────────────────
IF OBJECT_ID('Rooms') IS NULL
CREATE TABLE Rooms (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    RoomNumber      NVARCHAR(10)   NOT NULL UNIQUE,
    RoomType        INT            NOT NULL DEFAULT 1, -- 1=Standard 2=Deluxe 3=Suite 4=Presidential
    Floor           INT            NOT NULL,
    Status          INT            NOT NULL DEFAULT 1, -- 1=Available 2=Occupied 3=Dirty 4=Clean 5=Maintenance 6=Inspecting 7=DND
    RatePerNight    DECIMAL(18,2)  NOT NULL,
    MaxOccupancy    INT            NOT NULL DEFAULT 2,
    HasMinibar      BIT            NOT NULL DEFAULT 0,
    HasJacuzzi      BIT            NOT NULL DEFAULT 0,
    Description     NVARCHAR(500)  NULL,
    Notes           NVARCHAR(500)  NULL,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2      NULL,
    IsDeleted       BIT            NOT NULL DEFAULT 0,
    CreatedBy       INT            NULL,
    UpdatedBy       INT            NULL
);
GO

-- ── Reservations ───────────────────────────────────────────
IF OBJECT_ID('Reservations') IS NULL
CREATE TABLE Reservations (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    ReservationCode     NVARCHAR(30)   NOT NULL UNIQUE,
    GuestId             INT            NOT NULL REFERENCES Guests(Id),
    RoomId              INT            NOT NULL REFERENCES Rooms(Id),
    CheckIn             DATE           NOT NULL,
    CheckOut            DATE           NOT NULL,
    Nights              INT            NOT NULL,
    Adults              INT            NOT NULL DEFAULT 1,
    Children            INT            NOT NULL DEFAULT 0,
    RatePerNight        DECIMAL(18,2)  NOT NULL,
    Subtotal            DECIMAL(18,2)  NOT NULL,
    GstAmount           DECIMAL(18,2)  NOT NULL DEFAULT 0,
    TotalAmount         DECIMAL(18,2)  NOT NULL,
    AdvancePaid         DECIMAL(18,2)  NOT NULL DEFAULT 0,
    BalanceDue          DECIMAL(18,2)  NOT NULL DEFAULT 0,
    Status              INT            NOT NULL DEFAULT 1, -- 1=Pending 2=Confirmed 3=CheckedIn 4=CheckedOut 5=Cancelled 6=NoShow
    Channel             INT            NOT NULL DEFAULT 1, -- 1=Direct 2=BookingCom 3=MakeMyTrip 4=Agoda 5=WalkIn 6=Corporate
    PaymentMethod       INT            NOT NULL DEFAULT 1, -- 1=Cash 2=Card 3=UPI 4=BankTransfer 5=CityLedger 6=OTACollect
    SpecialRequests     NVARCHAR(1000) NULL,
    CancellationReason  NVARCHAR(500)  NULL,
    CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2      NULL,
    IsDeleted           BIT            NOT NULL DEFAULT 0,
    CreatedBy           INT            NULL,
    UpdatedBy           INT            NULL
);
GO

-- ── Invoices ───────────────────────────────────────────────
IF OBJECT_ID('Invoices') IS NULL
CREATE TABLE Invoices (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNumber   NVARCHAR(30)   NOT NULL UNIQUE,
    ReservationId   INT            NOT NULL REFERENCES Reservations(Id),
    GuestId         INT            NOT NULL REFERENCES Guests(Id),
    InvoiceDate     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    DueDate         DATETIME2      NOT NULL,
    Subtotal        DECIMAL(18,2)  NOT NULL,
    DiscountAmount  DECIMAL(18,2)  NOT NULL DEFAULT 0,
    GstAmount       DECIMAL(18,2)  NOT NULL DEFAULT 0,
    TotalAmount     DECIMAL(18,2)  NOT NULL,
    PaidAmount      DECIMAL(18,2)  NOT NULL DEFAULT 0,
    BalanceDue      DECIMAL(18,2)  NOT NULL DEFAULT 0,
    Status          INT            NOT NULL DEFAULT 1, -- 1=Draft 2=Pending 3=Paid 4=Overdue 5=Cancelled
    PaymentMethod   INT            NULL,
    PaidOn          DATETIME2      NULL,
    Notes           NVARCHAR(1000) NULL,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2      NULL,
    IsDeleted       BIT            NOT NULL DEFAULT 0,
    CreatedBy       INT            NULL,
    UpdatedBy       INT            NULL
);
GO

-- ── Invoice Line Items ─────────────────────────────────────
IF OBJECT_ID('InvoiceLineItems') IS NULL
CREATE TABLE InvoiceLineItems (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId   INT            NOT NULL REFERENCES Invoices(Id),
    Description NVARCHAR(300)  NOT NULL,
    Quantity    INT            NOT NULL DEFAULT 1,
    UnitPrice   DECIMAL(18,2)  NOT NULL,
    Amount      DECIMAL(18,2)  NOT NULL,
    GstRate     DECIMAL(5,2)   NOT NULL DEFAULT 18,
    GstAmount   DECIMAL(18,2)  NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted   BIT            NOT NULL DEFAULT 0
);
GO

-- ── Housekeeping Tasks ─────────────────────────────────────
IF OBJECT_ID('HousekeepingTasks') IS NULL
CREATE TABLE HousekeepingTasks (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    RoomId          INT            NOT NULL REFERENCES Rooms(Id),
    AssignedToId    INT            NULL REFERENCES Users(Id),
    TaskType        NVARCHAR(50)   NOT NULL, -- Clean, Turndown, Inspection, DND
    Status          NVARCHAR(20)   NOT NULL DEFAULT 'Pending',
    Notes           NVARCHAR(500)  NULL,
    CompletedAt     DATETIME2      NULL,
    CreatedAt       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2      NULL,
    IsDeleted       BIT            NOT NULL DEFAULT 0
);
GO

-- ── Indexes ────────────────────────────────────────────────
CREATE INDEX IX_Reservations_GuestId   ON Reservations(GuestId);
CREATE INDEX IX_Reservations_RoomId    ON Reservations(RoomId);
CREATE INDEX IX_Reservations_Status    ON Reservations(Status);
CREATE INDEX IX_Reservations_CheckIn   ON Reservations(CheckIn);
CREATE INDEX IX_Invoices_GuestId       ON Invoices(GuestId);
CREATE INDEX IX_Invoices_Status        ON Invoices(Status);
CREATE INDEX IX_Invoices_Date          ON Invoices(InvoiceDate);
CREATE INDEX IX_Guests_Email           ON Guests(Email);
CREATE INDEX IX_Users_Email            ON Users(Email);
GO

-- ── Seed Data ──────────────────────────────────────────────
-- Admin user (password: Admin@123)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email='admin@hotelbilling.com')
INSERT INTO Users (FullName, Email, Phone, PasswordHash, Role, IsActive)
VALUES ('Super Admin', 'admin@hotelbilling.com', '+91 98765 00000',
        '$2a$11$rBnqQ3jK3VUiXZ0Q8xV9QeZ7Yp3n5fHmA2kL1DcWgNs4PoX6R8YtO', 1, 1);

-- Front desk user (password: Desk@123)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email='frontdesk@hotelbilling.com')
INSERT INTO Users (FullName, Email, Phone, PasswordHash, Role, IsActive)
VALUES ('Amit Sharma', 'frontdesk@hotelbilling.com', '+91 98765 43210',
        '$2a$11$rBnqQ3jK3VUiXZ0Q8xV9QeZ7Yp3n5fHmA2kL1DcWgNs4PoX6R8YtO', 3, 1);

-- Seed Rooms
IF NOT EXISTS (SELECT 1 FROM Rooms WHERE RoomNumber='101')
BEGIN
    INSERT INTO Rooms (RoomNumber, RoomType, Floor, RatePerNight, MaxOccupancy, Status) VALUES
    ('101', 1, 1, 4200, 2, 4), ('102', 1, 1, 4200, 2, 4), ('103', 1, 1, 4200, 2, 1),
    ('204', 1, 2, 4600, 2, 3), ('205', 1, 2, 4600, 2, 4),
    ('301', 2, 3, 6200, 3, 2), ('302', 2, 3, 6200, 3, 4), ('303', 2, 3, 6700, 3, 4),
    ('410', 2, 4, 6700, 3, 2), ('411', 2, 4, 6700, 3, 1),
    ('512', 3, 5, 10500, 4, 2), ('513', 3, 5, 10500, 4, 4),
    ('601', 3, 6, 10500, 4, 2), ('602', 3, 6, 14000, 4, 1);
END
GO
