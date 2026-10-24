-- =========================================================================
-- Accounts Module Schema Setup for GCTL.Framework
-- Creates supporting tables and ensures columns for Chart of Accounts,
-- Voucher Entry, Opening Balance, and Chart of Accounts Reports.
-- =========================================================================

-- 1. Acc_GroupLedger table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Acc_GroupLedger')
BEGIN
    CREATE TABLE dbo.Acc_GroupLedger (
        autoId NUMERIC(18,0) IDENTITY(1,1) NOT NULL,
        GroupLedgerCodeNo NVARCHAR(50) NOT NULL PRIMARY KEY,
        GroupLedgerName NVARCHAR(100) NOT NULL,
        ShortName NVARCHAR(50) NULL,
        IsActive VARCHAR(1) NULL CONSTRAINT DF_Acc_GroupLedger_IsActive DEFAULT ('1'),
        LUser NCHAR(100) NULL,
        LDate SMALLDATETIME NULL,
        LIP NVARCHAR(100) NULL,
        LMAC NVARCHAR(150) NULL,
        ModifyDate SMALLDATETIME NULL
    );

    INSERT INTO dbo.Acc_GroupLedger (GroupLedgerCodeNo, GroupLedgerName, ShortName, IsActive, LDate)
    VALUES 
        ('1', 'Assets', 'AST', '1', GETDATE()),
        ('2', 'Equities & Liabilities', 'EQ_LIB', '1', GETDATE()),
        ('3', 'Income', 'INC', '1', GETDATE()),
        ('4', 'Expenses', 'EXP', '1', GETDATE());
END
GO

-- 2. Add columns to Acc_ControlLedger
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_ControlLedger' AND COLUMN_NAME = 'GroupLedgerCodeNo')
BEGIN
    ALTER TABLE dbo.Acc_ControlLedger ADD GroupLedgerCodeNo NVARCHAR(50) NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_ControlLedger' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE dbo.Acc_ControlLedger ADD IsActive VARCHAR(1) NULL CONSTRAINT DF_Acc_ControlLedger_IsActive DEFAULT ('1');
    EXEC('UPDATE dbo.Acc_ControlLedger SET IsActive = ''1'' WHERE IsActive IS NULL;');
END
GO

-- 3. Add columns to Acc_SubControlLedger
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_SubControlLedger' AND COLUMN_NAME = 'GroupLedgerCodeNo')
BEGIN
    ALTER TABLE dbo.Acc_SubControlLedger ADD GroupLedgerCodeNo NVARCHAR(50) NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_SubControlLedger' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE dbo.Acc_SubControlLedger ADD IsActive VARCHAR(1) NULL CONSTRAINT DF_Acc_SubControlLedger_IsActive DEFAULT ('1');
    EXEC('UPDATE dbo.Acc_SubControlLedger SET IsActive = ''1'' WHERE IsActive IS NULL;');
END
GO

-- 4. Add columns to Acc_SubSubsidiaryLedger
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_SubSubsidiaryLedger' AND COLUMN_NAME = 'SubsidiaryLedgerCodeNo')
BEGIN
    ALTER TABLE dbo.Acc_SubSubsidiaryLedger ADD SubsidiaryLedgerCodeNo NVARCHAR(50) NULL;
    EXEC('UPDATE dbo.Acc_SubSubsidiaryLedger SET SubsidiaryLedgerCodeNo = SubSusidiaryLedgerCodeNo WHERE SubsidiaryLedgerCodeNo IS NULL;');
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_SubSubsidiaryLedger' AND COLUMN_NAME = 'SubControlLedgerCodeNo')
BEGIN
    ALTER TABLE dbo.Acc_SubSubsidiaryLedger ADD SubControlLedgerCodeNo NVARCHAR(50) NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_SubSubsidiaryLedger' AND COLUMN_NAME = 'ControlLedgerCodeNo')
BEGIN
    ALTER TABLE dbo.Acc_SubSubsidiaryLedger ADD ControlLedgerCodeNo NVARCHAR(50) NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_SubSubsidiaryLedger' AND COLUMN_NAME = 'GroupLedgerCodeNo')
BEGIN
    ALTER TABLE dbo.Acc_SubSubsidiaryLedger ADD GroupLedgerCodeNo NVARCHAR(50) NULL;
END
GO

-- 5. Add columns to Acc_GeneralLedger
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_GeneralLedger' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE dbo.Acc_GeneralLedger ADD IsActive VARCHAR(1) NULL CONSTRAINT DF_Acc_GeneralLedger_IsActive DEFAULT ('1');
    EXEC('UPDATE dbo.Acc_GeneralLedger SET IsActive = ''1'' WHERE IsActive IS NULL;');
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_GeneralLedger' AND COLUMN_NAME = 'GroupLedgerCodeNo')
BEGIN
    ALTER TABLE dbo.Acc_GeneralLedger ADD GroupLedgerCodeNo NVARCHAR(50) NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_GeneralLedger' AND COLUMN_NAME = 'ControlLedgerCodeNo')
BEGIN
    ALTER TABLE dbo.Acc_GeneralLedger ADD ControlLedgerCodeNo NVARCHAR(50) NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_GeneralLedger' AND COLUMN_NAME = 'SubsidiaryLedgerCodeNo')
BEGIN
    ALTER TABLE dbo.Acc_GeneralLedger ADD SubsidiaryLedgerCodeNo NVARCHAR(50) NULL;
END
GO

-- 6. Acc_FinancialYear table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Acc_FinancialYear')
BEGIN
    CREATE TABLE dbo.Acc_FinancialYear (
        autoId NUMERIC(18,0) IDENTITY(1,1) NOT NULL,
        FinancialCodeNo NVARCHAR(50) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        StartDate DATETIME NULL,
        EndDate DATETIME NULL,
        CreatedBy INT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_Acc_FinancialYear_CreatedAt DEFAULT (GETDATE()),
        UpdatedBy INT NULL,
        UpdatedAt DATETIME NULL,
        LIP NVARCHAR(50) NULL,
        LMAC NVARCHAR(50) NULL,
        IsArchived BIT NULL CONSTRAINT DF_Acc_FinancialYear_IsArchived DEFAULT (0)
    );

    INSERT INTO dbo.Acc_FinancialYear (FinancialCodeNo, Name, StartDate, EndDate)
    VALUES 
        ('001', '2024-2025', '2024-07-01', '2025-06-30'),
        ('002', '2025-2026', '2025-07-01', '2026-06-30'),
        ('003', '2026-2027', '2026-07-01', '2027-06-30');
END
GO

-- 7. Add columns to Acc_Company_Opening_Balance
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_Company_Opening_Balance' AND COLUMN_NAME = 'FinancialCodeNo')
BEGIN
    ALTER TABLE dbo.Acc_Company_Opening_Balance ADD FinancialCodeNo NVARCHAR(50) NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_Company_Opening_Balance' AND COLUMN_NAME = 'GeneralLedgerCodeNo')
BEGIN
    ALTER TABLE dbo.Acc_Company_Opening_Balance ADD GeneralLedgerCodeNo NVARCHAR(50) NULL;
    EXEC('UPDATE dbo.Acc_Company_Opening_Balance SET GeneralLedgerCodeNo = SubSusidiaryLedgerCodeNo WHERE GeneralLedgerCodeNo IS NULL;');
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_Company_Opening_Balance' AND COLUMN_NAME = 'Reference')
BEGIN
    ALTER TABLE dbo.Acc_Company_Opening_Balance ADD Reference NVARCHAR(250) NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_Company_Opening_Balance' AND COLUMN_NAME = 'Description')
BEGIN
    ALTER TABLE dbo.Acc_Company_Opening_Balance ADD Description NVARCHAR(MAX) NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_Company_Opening_Balance' AND COLUMN_NAME = 'CreatedAt')
BEGIN
    ALTER TABLE dbo.Acc_Company_Opening_Balance ADD CreatedAt DATETIME NULL CONSTRAINT DF_Acc_Company_Opening_Balance_CreatedAt DEFAULT (GETDATE());
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_Company_Opening_Balance' AND COLUMN_NAME = 'CreatedBy')
BEGIN
    ALTER TABLE dbo.Acc_Company_Opening_Balance ADD CreatedBy INT NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_Company_Opening_Balance' AND COLUMN_NAME = 'DeletedAt')
BEGIN
    ALTER TABLE dbo.Acc_Company_Opening_Balance ADD DeletedAt DATETIME NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_Company_Opening_Balance' AND COLUMN_NAME = 'DeletedBy')
BEGIN
    ALTER TABLE dbo.Acc_Company_Opening_Balance ADD DeletedBy INT NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_Company_Opening_Balance' AND COLUMN_NAME = 'UpdatedAt')
BEGIN
    ALTER TABLE dbo.Acc_Company_Opening_Balance ADD UpdatedAt DATETIME NULL;
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_Company_Opening_Balance' AND COLUMN_NAME = 'UpdatedBy')
BEGIN
    ALTER TABLE dbo.Acc_Company_Opening_Balance ADD UpdatedBy INT NULL;
END
GO

-- 8. Add IsActive to Acc_VoucherEntry and Acc_VoucherEntryDetails
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_VoucherEntry' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE dbo.Acc_VoucherEntry ADD IsActive NVARCHAR(10) NULL CONSTRAINT DF_Acc_VoucherEntry_IsActive DEFAULT ('1');
    EXEC('UPDATE dbo.Acc_VoucherEntry SET IsActive = ''1'' WHERE IsActive IS NULL;');
END
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Acc_VoucherEntryDetails' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE dbo.Acc_VoucherEntryDetails ADD IsActive NVARCHAR(10) NULL CONSTRAINT DF_Acc_VoucherEntryDetails_IsActive DEFAULT ('1');
    EXEC('UPDATE dbo.Acc_VoucherEntryDetails SET IsActive = ''1'' WHERE IsActive IS NULL;');
END
GO

-- 9. Acc_VoucherInvoicePaymentReceived table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Acc_VoucherInvoicePaymentReceived')
BEGIN
    CREATE TABLE dbo.Acc_VoucherInvoicePaymentReceived (
        autoId DECIMAL(18,0) IDENTITY(1,1) PRIMARY KEY,
        VoucherNo VARCHAR(50) NULL,
        InvoiceNo VARCHAR(50) NULL,
        Amount DECIMAL(18,2) NULL,
        IsActive VARCHAR(1) DEFAULT '1',
        LUser VARCHAR(50) NULL,
        LDate DATETIME DEFAULT GETDATE(),
        LIP VARCHAR(50) NULL,
        LMAC VARCHAR(50) NULL,
        ModifyDate DATETIME NULL
    );
END
GO

