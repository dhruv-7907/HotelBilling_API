USE [HotelBillingDB];
GO

-- ================================================================
-- 003_MultiTenantSubscriptionFoundation.sql
-- Multi-tenant + subscription foundation (non-breaking, incremental)
-- ================================================================

-- -----------------------------
-- 1) Tenant master
-- -----------------------------
IF OBJECT_ID('dbo.Tenants', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenants
    (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        TenantCode          NVARCHAR(50)  NOT NULL,
        TenantName          NVARCHAR(200) NOT NULL,
        TenantType          NVARCHAR(30)  NOT NULL DEFAULT 'Hotel', -- Hotel | Resort | Waterpark | Mixed
        ContactEmail        NVARCHAR(200) NULL,
        ContactPhone        NVARCHAR(30)  NULL,
        IsActive            BIT           NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2     NULL,
        IsDeleted           BIT           NOT NULL DEFAULT 0
    );

    CREATE UNIQUE INDEX UQ_Tenants_TenantCode ON dbo.Tenants(TenantCode);
    CREATE UNIQUE INDEX UQ_Tenants_TenantName ON dbo.Tenants(TenantName) WHERE IsDeleted = 0;
END
GO

-- -----------------------------
-- 2) Properties (hotel/resort/waterpark units)
-- -----------------------------
IF OBJECT_ID('dbo.Properties', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Properties
    (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        TenantId            INT           NOT NULL,
        PropertyCode        NVARCHAR(50)  NOT NULL,
        PropertyName        NVARCHAR(200) NOT NULL,
        PropertyType        NVARCHAR(30)  NOT NULL DEFAULT 'Hotel', -- Hotel | Resort | Waterpark
        TimeZoneId          NVARCHAR(100) NOT NULL DEFAULT 'Asia/Kolkata',
        CurrencyCode        NVARCHAR(10)  NOT NULL DEFAULT 'INR',
        AddressLine1        NVARCHAR(300) NULL,
        City                NVARCHAR(120) NULL,
        State               NVARCHAR(120) NULL,
        Country             NVARCHAR(120) NULL,
        PostalCode          NVARCHAR(20)  NULL,
        IsActive            BIT           NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2     NULL,
        IsDeleted           BIT           NOT NULL DEFAULT 0,

        CONSTRAINT FK_Properties_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
    );

    CREATE UNIQUE INDEX UQ_Properties_Tenant_PropertyCode
        ON dbo.Properties(TenantId, PropertyCode)
        WHERE IsDeleted = 0;
END
GO

-- -----------------------------
-- 3) Subscription plans
-- -----------------------------
IF OBJECT_ID('dbo.SubscriptionPlans', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SubscriptionPlans
    (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        PlanCode            NVARCHAR(50)   NOT NULL,
        PlanName            NVARCHAR(120)  NOT NULL,
        BillingCycle        TINYINT        NOT NULL,  -- 1=Monthly, 2=Yearly
        PriceAmount         DECIMAL(18,2)  NOT NULL DEFAULT 0,
        MaxUsers            INT            NOT NULL DEFAULT 5,
        MaxProperties       INT            NOT NULL DEFAULT 1,
        FeaturesJson        NVARCHAR(MAX)  NULL,
        IsActive            BIT            NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2      NULL,
        IsDeleted           BIT            NOT NULL DEFAULT 0,

        CONSTRAINT CK_SubscriptionPlans_BillingCycle CHECK (BillingCycle IN (1,2)),
        CONSTRAINT CK_SubscriptionPlans_MaxUsers CHECK (MaxUsers > 0),
        CONSTRAINT CK_SubscriptionPlans_MaxProperties CHECK (MaxProperties > 0),
        CONSTRAINT CK_SubscriptionPlans_Price CHECK (PriceAmount >= 0)
    );

    CREATE UNIQUE INDEX UQ_SubscriptionPlans_PlanCode ON dbo.SubscriptionPlans(PlanCode);
END
GO

-- -----------------------------
-- 4) Tenant subscriptions
-- -----------------------------
IF OBJECT_ID('dbo.TenantSubscriptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantSubscriptions
    (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        TenantId            INT            NOT NULL,
        PlanId              INT            NOT NULL,
        Status              TINYINT        NOT NULL DEFAULT 1, -- 1=Trial,2=Active,3=PastDue,4=Suspended,5=Cancelled
        StartDate           DATE           NOT NULL,
        EndDate             DATE           NULL,
        TrialEndsAt         DATETIME2      NULL,
        BillingPeriodStart  DATE           NULL,
        BillingPeriodEnd    DATE           NULL,
        NextBillingAt       DATETIME2      NULL,
        AutoRenew           BIT            NOT NULL DEFAULT 1,
        CancelledAt         DATETIME2      NULL,
        CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2      NULL,
        IsDeleted           BIT            NOT NULL DEFAULT 0,

        CONSTRAINT FK_TenantSubscriptions_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_TenantSubscriptions_PlanId FOREIGN KEY (PlanId) REFERENCES dbo.SubscriptionPlans(Id),
        CONSTRAINT CK_TenantSubscriptions_Status CHECK (Status BETWEEN 1 AND 5)
    );

    CREATE INDEX IX_TenantSubscriptions_Tenant_Status
        ON dbo.TenantSubscriptions(TenantId, Status)
        WHERE IsDeleted = 0;
END
GO

-- -----------------------------
-- 5) Tenant-user mapping
-- -----------------------------
IF OBJECT_ID('dbo.TenantUsers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantUsers
    (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        TenantId            INT           NOT NULL,
        UserId              INT           NOT NULL,
        Role                INT           NOT NULL, -- mirrors app role enum
        IsPrimary           BIT           NOT NULL DEFAULT 0,
        IsActive            BIT           NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2     NULL,
        IsDeleted           BIT           NOT NULL DEFAULT 0,

        CONSTRAINT FK_TenantUsers_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_TenantUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );

    CREATE UNIQUE INDEX UQ_TenantUsers_Tenant_User
        ON dbo.TenantUsers(TenantId, UserId)
        WHERE IsDeleted = 0;
END
GO

-- -----------------------------
-- 6) Non-breaking tenancy columns on existing tables
-- -----------------------------
IF COL_LENGTH('dbo.Users', 'TenantId') IS NULL
    ALTER TABLE dbo.Users ADD TenantId INT NULL;
GO

IF COL_LENGTH('dbo.Guests', 'TenantId') IS NULL
    ALTER TABLE dbo.Guests ADD TenantId INT NULL;
GO

IF COL_LENGTH('dbo.Rooms', 'TenantId') IS NULL
    ALTER TABLE dbo.Rooms ADD TenantId INT NULL;
GO

IF COL_LENGTH('dbo.Rooms', 'PropertyId') IS NULL
    ALTER TABLE dbo.Rooms ADD PropertyId INT NULL;
GO

IF COL_LENGTH('dbo.Reservations', 'TenantId') IS NULL
    ALTER TABLE dbo.Reservations ADD TenantId INT NULL;
GO

IF COL_LENGTH('dbo.Reservations', 'PropertyId') IS NULL
    ALTER TABLE dbo.Reservations ADD PropertyId INT NULL;
GO

IF COL_LENGTH('dbo.Invoices', 'TenantId') IS NULL
    ALTER TABLE dbo.Invoices ADD TenantId INT NULL;
GO

IF COL_LENGTH('dbo.Invoices', 'PropertyId') IS NULL
    ALTER TABLE dbo.Invoices ADD PropertyId INT NULL;
GO

IF COL_LENGTH('dbo.HousekeepingTasks', 'TenantId') IS NULL
    ALTER TABLE dbo.HousekeepingTasks ADD TenantId INT NULL;
GO

IF COL_LENGTH('dbo.HousekeepingTasks', 'PropertyId') IS NULL
    ALTER TABLE dbo.HousekeepingTasks ADD PropertyId INT NULL;
GO

-- Add FKs only if missing
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_TenantId')
    ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Guests_TenantId')
    ALTER TABLE dbo.Guests ADD CONSTRAINT FK_Guests_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Rooms_TenantId')
    ALTER TABLE dbo.Rooms ADD CONSTRAINT FK_Rooms_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Rooms_PropertyId')
    ALTER TABLE dbo.Rooms ADD CONSTRAINT FK_Rooms_PropertyId FOREIGN KEY (PropertyId) REFERENCES dbo.Properties(Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Reservations_TenantId')
    ALTER TABLE dbo.Reservations ADD CONSTRAINT FK_Reservations_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Reservations_PropertyId')
    ALTER TABLE dbo.Reservations ADD CONSTRAINT FK_Reservations_PropertyId FOREIGN KEY (PropertyId) REFERENCES dbo.Properties(Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Invoices_TenantId')
    ALTER TABLE dbo.Invoices ADD CONSTRAINT FK_Invoices_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Invoices_PropertyId')
    ALTER TABLE dbo.Invoices ADD CONSTRAINT FK_Invoices_PropertyId FOREIGN KEY (PropertyId) REFERENCES dbo.Properties(Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HousekeepingTasks_TenantId')
    ALTER TABLE dbo.HousekeepingTasks ADD CONSTRAINT FK_HousekeepingTasks_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HousekeepingTasks_PropertyId')
    ALTER TABLE dbo.HousekeepingTasks ADD CONSTRAINT FK_HousekeepingTasks_PropertyId FOREIGN KEY (PropertyId) REFERENCES dbo.Properties(Id);
GO

-- Helpful tenancy indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Rooms_Tenant_Property' AND object_id = OBJECT_ID('dbo.Rooms'))
    CREATE NONCLUSTERED INDEX IX_Rooms_Tenant_Property ON dbo.Rooms(TenantId, PropertyId) WHERE IsDeleted = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_Tenant_Property_Dates' AND object_id = OBJECT_ID('dbo.Reservations'))
    CREATE NONCLUSTERED INDEX IX_Reservations_Tenant_Property_Dates ON dbo.Reservations(TenantId, PropertyId, CheckIn, CheckOut) WHERE IsDeleted = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Invoices_Tenant_Property_Date' AND object_id = OBJECT_ID('dbo.Invoices'))
    CREATE NONCLUSTERED INDEX IX_Invoices_Tenant_Property_Date ON dbo.Invoices(TenantId, PropertyId, InvoiceDate) WHERE IsDeleted = 0;
GO

-- -----------------------------
-- 7) Starter plan seed data
-- -----------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.SubscriptionPlans WHERE PlanCode = 'BASIC')
INSERT INTO dbo.SubscriptionPlans (PlanCode, PlanName, BillingCycle, PriceAmount, MaxUsers, MaxProperties, FeaturesJson)
VALUES ('BASIC', 'Basic', 1, 4999.00, 10, 1, N'{"reports":"standard","api":false,"waterpark":false}');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SubscriptionPlans WHERE PlanCode = 'PRO')
INSERT INTO dbo.SubscriptionPlans (PlanCode, PlanName, BillingCycle, PriceAmount, MaxUsers, MaxProperties, FeaturesJson)
VALUES ('PRO', 'Pro', 1, 14999.00, 50, 5, N'{"reports":"advanced","api":true,"waterpark":true}');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SubscriptionPlans WHERE PlanCode = 'ENTERPRISE')
INSERT INTO dbo.SubscriptionPlans (PlanCode, PlanName, BillingCycle, PriceAmount, MaxUsers, MaxProperties, FeaturesJson)
VALUES ('ENTERPRISE', 'Enterprise', 2, 149999.00, 500, 100, N'{"reports":"advanced","api":true,"waterpark":true,"sso":true}');
GO

