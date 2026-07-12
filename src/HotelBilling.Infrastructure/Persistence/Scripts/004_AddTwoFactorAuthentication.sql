-- ============================================================
-- Hotel Billing Pro — Two-Factor Authentication Schema Update
-- SQL Server 2019 / Azure SQL
-- ============================================================

USE HotelBillingDB;
GO

-- ── Users Table Updates ─────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'TwoFactorEnabled')
BEGIN
    ALTER TABLE Users ADD TwoFactorEnabled BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'TwoFactorSecret')
BEGIN
    ALTER TABLE Users ADD TwoFactorSecret NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'TwoFactorVerifiedAt')
BEGIN
    ALTER TABLE Users ADD TwoFactorVerifiedAt DATETIME2 NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'FailedOtpAttempts')
BEGIN
    ALTER TABLE Users ADD FailedOtpAttempts INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'OtpLockedUntil')
BEGIN
    ALTER TABLE Users ADD OtpLockedUntil DATETIME2 NULL;
END
GO

-- ── RecoveryCodes Table ──────────────────────────────────────
IF OBJECT_ID('RecoveryCodes') IS NULL
BEGIN
    CREATE TABLE RecoveryCodes (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        UserId      INT NOT NULL REFERENCES Users(Id),
        CodeHash    NVARCHAR(255) NOT NULL,
        Used        BIT NOT NULL DEFAULT 0,
        UsedAt      DATETIME2 NULL,
        CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    CREATE INDEX IX_RecoveryCodes_UserId ON RecoveryCodes(UserId);
END
GO
