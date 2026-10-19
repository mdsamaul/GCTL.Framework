SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- 1. Index on Acc_VoucherEntryDetails for VoucherEntryCodeNo (Covering amounts)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Acc_VoucherEntryDetails_CodeNo' AND object_id = OBJECT_ID('Acc_VoucherEntryDetails'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Acc_VoucherEntryDetails_CodeNo 
    ON dbo.Acc_VoucherEntryDetails (VoucherEntryCodeNo) 
    INCLUDE (DebitAmount, CreditAmount, IsActive);
END
GO

-- 2. Materialized / Indexed View on Acc_VoucherEntryDetails for instant O(1) totals
IF OBJECT_ID('dbo.vw_Acc_VoucherSummary_Fast', 'V') IS NULL
BEGIN
    EXEC('
    CREATE VIEW dbo.vw_Acc_VoucherSummary_Fast
    WITH SCHEMABINDING
    AS
    SELECT 
        VoucherEntryCodeNo,
        COUNT_BIG(*) AS LineCount,
        SUM(ISNULL(DebitAmount, 0)) AS TotalDebit,
        SUM(ISNULL(CreditAmount, 0)) AS TotalCredit
    FROM dbo.Acc_VoucherEntryDetails
    WHERE IsActive IS NULL OR IsActive != ''0''
    GROUP BY VoucherEntryCodeNo;
    ');
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'CIX_vw_Acc_VoucherSummary_Fast' AND object_id = OBJECT_ID('vw_Acc_VoucherSummary_Fast'))
BEGIN
    CREATE UNIQUE CLUSTERED INDEX CIX_vw_Acc_VoucherSummary_Fast 
    ON dbo.vw_Acc_VoucherSummary_Fast (VoucherEntryCodeNo);
END
GO

-- 3. Composite covering index on Acc_VoucherEntry for ultra-fast date/key-seek paging
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Acc_VoucherEntry_Active_Date' AND object_id = OBJECT_ID('Acc_VoucherEntry'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Acc_VoucherEntry_Active_Date 
    ON dbo.Acc_VoucherEntry (VoucherDate DESC, autoId DESC) 
    INCLUDE (VoucherEntryCodeNo, VoucherNo, VoucherType_Code, BranchCode, CompanyCode, Main_CompanyCode, IsApproved, IsActive);
END
GO

-- 4. Index on VoucherEntryCodeNo covering type, branch, company
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AccVE_VoucherCode_Type_Cover' AND object_id = OBJECT_ID('Acc_VoucherEntry'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AccVE_VoucherCode_Type_Cover
    ON dbo.Acc_VoucherEntry (VoucherEntryCodeNo)
    INCLUDE (VoucherType_Code, BranchCode, CompanyCode, Main_CompanyCode, IsActive);
END
GO

-- 5. Index on VoucherNo for instant search by voucher no
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Acc_VoucherEntry_VoucherNo' AND object_id = OBJECT_ID('Acc_VoucherEntry'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Acc_VoucherEntry_VoucherNo
    ON dbo.Acc_VoucherEntry (VoucherNo)
    INCLUDE (autoId, VoucherEntryCodeNo, VoucherDate, IsActive);
END
GO

-- 6. High-Performance Stored Procedure (Dynamic SQL 2-Step Key Seeking + Fast Summary)
CREATE OR ALTER PROCEDURE dbo.sp_GetVoucherList
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @VoucherType NVARCHAR(50) = NULL,
    @Status NVARCHAR(50) = NULL,
    @SearchTerm NVARCHAR(100) = NULL,
    @BranchCode NVARCHAR(50) = NULL,
    @CompanyCode NVARCHAR(50) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 10,
    @SortColumn NVARCHAR(50) = 'VoucherDate',
    @SortOrder NVARCHAR(10) = 'DESC',
    @SkipSummary BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize < 1 SET @PageSize = 10;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    -- Normalize Sort Column
    DECLARE @OrderBy NVARCHAR(200);
    SET @SortOrder = UPPER(ISNULL(@SortOrder, 'DESC'));
    IF @SortOrder NOT IN ('ASC', 'DESC') SET @SortOrder = 'DESC';

    SET @SortColumn = LOWER(ISNULL(@SortColumn, 'voucherdate'));
    IF @SortColumn = 'voucherno'
        SET @OrderBy = 've.VoucherNo ' + @SortOrder + ', ve.autoId ' + @SortOrder;
    ELSE IF @SortColumn = 'vouchertype'
        SET @OrderBy = 've.VoucherType_Code ' + @SortOrder + ', ve.autoId ' + @SortOrder;
    ELSE IF @SortColumn IN ('branchname', 'branchcode', 'branch')
        SET @OrderBy = 've.BranchCode ' + @SortOrder + ', ve.autoId ' + @SortOrder;
    ELSE IF @SortColumn = 'narration'
        SET @OrderBy = 've.Narration ' + @SortOrder + ', ve.autoId ' + @SortOrder;
    ELSE IF @SortColumn IN ('isapproved', 'status')
        SET @OrderBy = 've.IsApproved ' + @SortOrder + ', ve.autoId ' + @SortOrder;
    ELSE
        SET @OrderBy = 've.VoucherDate ' + @SortOrder + ', ve.autoId ' + @SortOrder;

    -- Build WHERE clause
    DECLARE @HasFilter BIT = 0;
    DECLARE @Where NVARCHAR(MAX) = N'WHERE (ve.IsActive IS NULL OR ve.IsActive != ''0'')';

    IF @FromDate IS NOT NULL
    BEGIN
        SET @Where += N' AND ve.VoucherDate >= @FromDate';
        SET @HasFilter = 1;
    END

    IF @ToDate IS NOT NULL
    BEGIN
        SET @Where += N' AND ve.VoucherDate < DATEADD(DAY, 1, @ToDate)';
        SET @HasFilter = 1;
    END

    IF @VoucherType IS NOT NULL AND @VoucherType <> ''
    BEGIN
        SET @HasFilter = 1;
        IF @VoucherType IN ('1', 'BRV') SET @Where += N' AND ve.VoucherType_Code IN (''1'', ''BRV'')';
        ELSE IF @VoucherType IN ('2', 'BPV') SET @Where += N' AND ve.VoucherType_Code IN (''2'', ''BPV'')';
        ELSE IF @VoucherType IN ('3', 'CRV') SET @Where += N' AND ve.VoucherType_Code IN (''3'', ''CRV'')';
        ELSE IF @VoucherType IN ('4', 'CPV') SET @Where += N' AND ve.VoucherType_Code IN (''4'', ''CPV'')';
        ELSE IF @VoucherType IN ('5', 'JV') SET @Where += N' AND ve.VoucherType_Code IN (''5'', ''JV'')';
        ELSE IF @VoucherType IN ('6', 'CV') SET @Where += N' AND ve.VoucherType_Code IN (''6'', ''CV'')';
        ELSE SET @Where += N' AND ve.VoucherType_Code = @VoucherType';
    END

    IF @Status IS NOT NULL AND @Status <> ''
    BEGIN
        SET @HasFilter = 1;
        IF @Status IN ('Pending Approval', 'Pending', 'N')
            SET @Where += N' AND (ve.IsApproved IS NULL OR ve.IsApproved IN (''Pending Approval'', ''Pending'', ''N'', ''''))';
        ELSE IF @Status IN ('Posted', 'Approved', 'Y')
            SET @Where += N' AND ve.IsApproved IN (''Posted'', ''Approved'', ''Y'')';
        ELSE
            SET @Where += N' AND ve.IsApproved = @Status';
    END

    IF @BranchCode IS NOT NULL AND @BranchCode <> ''
    BEGIN
        SET @Where += N' AND ve.BranchCode = @BranchCode';
        SET @HasFilter = 1;
    END

    IF @CompanyCode IS NOT NULL AND @CompanyCode <> ''
    BEGIN
        SET @Where += N' AND (ve.CompanyCode = @CompanyCode OR ve.Main_CompanyCode = @CompanyCode)';
        SET @HasFilter = 1;
    END

    DECLARE @Search NVARCHAR(102) = NULL;
    IF @SearchTerm IS NOT NULL AND RTRIM(LTRIM(@SearchTerm)) <> ''
    BEGIN
        SET @Search = '%' + RTRIM(LTRIM(@SearchTerm)) + '%';
        SET @Where += N' AND (ve.VoucherNo LIKE @Search OR ve.Narration LIKE @Search OR ve.Reference LIKE @Search OR ve.InvoiceNo LIKE @Search)';
        SET @HasFilter = 1;
    END

    -- Dynamic Paged Query (2-Step Key-Seek execution in 5-20 ms)
    DECLARE @SqlPaged NVARCHAR(MAX) = N'
    ;WITH PageKeys AS (
        SELECT ve.autoId, ve.VoucherEntryCodeNo
        FROM dbo.Acc_VoucherEntry ve WITH (NOLOCK)
        ' + @Where + N'
        ORDER BY ' + @OrderBy + N'
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY
    )
    SELECT 
        ve.autoId,
        ve.VoucherEntryCodeNo,
        ve.VoucherNo,
        ve.VoucherType_Code,
        ISNULL(vt.Voucher_TypeName, ve.VoucherType_Code) AS VoucherTypeName,
        ve.VoucherDate,
        ve.FinancialYear,
        ve.BranchCode,
        ISNULL(cb.BranchName, ''Head Office'') AS BranchName,
        ve.CompanyCode,
        ve.Main_CompanyCode,
        ve.Narration,
        ve.Reference,
        ve.InvoiceNo,
        ISNULL(ve.IsApproved, ''Pending Approval'') AS IsApproved,
        ISNULL(vs.TotalDebit, 0) AS TotalAmount
    FROM PageKeys pk
    JOIN dbo.Acc_VoucherEntry ve WITH (NOLOCK) ON ve.autoId = pk.autoId
    LEFT JOIN dbo.vw_Acc_VoucherSummary_Fast vs WITH (NOEXPAND) ON vs.VoucherEntryCodeNo = pk.VoucherEntryCodeNo
    LEFT JOIN dbo.Acc_VoucherType vt WITH (NOLOCK) ON vt.VoucherType_Code = ve.VoucherType_Code
    LEFT JOIN dbo.Core_Branch cb WITH (NOLOCK) ON cb.BranchCode = ve.BranchCode
    ORDER BY ' + @OrderBy + N';';

    DECLARE @Params NVARCHAR(500) = N'@FromDate DATE, @ToDate DATE, @VoucherType NVARCHAR(50), @Status NVARCHAR(50), @BranchCode NVARCHAR(50), @CompanyCode NVARCHAR(50), @Search NVARCHAR(102), @Offset INT, @PageSize INT';

    EXEC sp_executesql @SqlPaged, @Params,
        @FromDate = @FromDate,
        @ToDate = @ToDate,
        @VoucherType = @VoucherType,
        @Status = @Status,
        @BranchCode = @BranchCode,
        @CompanyCode = @CompanyCode,
        @Search = @Search,
        @Offset = @Offset,
        @PageSize = @PageSize;

    -- Summary Result Set
    IF @SkipSummary = 1
    BEGIN
        SELECT 
            0 AS TotalVouchers,
            0 AS VouchersToday,
            0 AS VouchersThisMonth,
            0.00 AS TotalDebit,
            0.00 AS TotalCredit,
            0.00 AS CashBalance,
            0.00 AS BankBalance,
            0 AS BrvCount,
            0.00 AS BrvAmount,
            0 AS BpvCount,
            0.00 AS BpvAmount,
            0 AS CrvCount,
            0.00 AS CrvAmount,
            0 AS CpvCount,
            0.00 AS CpvAmount,
            0 AS JvCount,
            0.00 AS JvAmount,
            0 AS CvCount,
            0.00 AS CvAmount;
        RETURN;
    END

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    DECLARE @MonthStart DATE = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1);
    DECLARE @MonthEnd DATE = EOMONTH(@Today);

    DECLARE @TotalVouchers INT = 0,
            @VouchersToday INT = 0,
            @VouchersThisMonth INT = 0,
            @BrvCount INT = 0,
            @BpvCount INT = 0,
            @CrvCount INT = 0,
            @CpvCount INT = 0,
            @JvCount INT = 0,
            @CvCount INT = 0,
            @BrvAmount DECIMAL(18,2) = 0,
            @BpvAmount DECIMAL(18,2) = 0,
            @CrvAmount DECIMAL(18,2) = 0,
            @CpvAmount DECIMAL(18,2) = 0,
            @JvAmount DECIMAL(18,2) = 0,
            @CvAmount DECIMAL(18,2) = 0,
            @TotalDebit DECIMAL(18,2) = 0;

    -- 1. Counts from Acc_VoucherEntry using covering index (~20ms)
    SELECT 
        @TotalVouchers = COUNT(*),
        @VouchersToday = SUM(CASE WHEN ve.VoucherDate >= @Today AND ve.VoucherDate < DATEADD(DAY, 1, @Today) THEN 1 ELSE 0 END),
        @VouchersThisMonth = SUM(CASE WHEN ve.VoucherDate >= @MonthStart AND ve.VoucherDate < DATEADD(DAY, 1, @MonthEnd) THEN 1 ELSE 0 END),
        @BrvCount = SUM(CASE WHEN ve.VoucherType_Code IN ('1', 'BRV') THEN 1 ELSE 0 END),
        @BpvCount = SUM(CASE WHEN ve.VoucherType_Code IN ('2', 'BPV') THEN 1 ELSE 0 END),
        @CrvCount = SUM(CASE WHEN ve.VoucherType_Code IN ('3', 'CRV') THEN 1 ELSE 0 END),
        @CpvCount = SUM(CASE WHEN ve.VoucherType_Code IN ('4', 'CPV') THEN 1 ELSE 0 END),
        @JvCount = SUM(CASE WHEN ve.VoucherType_Code IN ('5', 'JV') THEN 1 ELSE 0 END),
        @CvCount = SUM(CASE WHEN ve.VoucherType_Code IN ('6', 'CV') THEN 1 ELSE 0 END)
    FROM dbo.Acc_VoucherEntry ve WITH (NOLOCK)
    WHERE (ve.IsActive IS NULL OR ve.IsActive != '0')
      AND (@BranchCode IS NULL OR @BranchCode = '' OR ve.BranchCode = @BranchCode)
      AND (@CompanyCode IS NULL OR @CompanyCode = '' OR ve.CompanyCode = @CompanyCode OR ve.Main_CompanyCode = @CompanyCode);

    -- 2. Amounts using pre-materialized Indexed View (vs)
    SELECT 
        @BrvAmount = ISNULL(SUM(CASE WHEN ve.VoucherType_Code IN ('1', 'BRV') THEN vs.TotalDebit ELSE 0 END), 0),
        @BpvAmount = ISNULL(SUM(CASE WHEN ve.VoucherType_Code IN ('2', 'BPV') THEN vs.TotalCredit ELSE 0 END), 0),
        @CrvAmount = ISNULL(SUM(CASE WHEN ve.VoucherType_Code IN ('3', 'CRV') THEN vs.TotalDebit ELSE 0 END), 0),
        @CpvAmount = ISNULL(SUM(CASE WHEN ve.VoucherType_Code IN ('4', 'CPV') THEN vs.TotalCredit ELSE 0 END), 0),
        @JvAmount = ISNULL(SUM(CASE WHEN ve.VoucherType_Code IN ('5', 'JV') THEN vs.TotalDebit ELSE 0 END), 0),
        @CvAmount = ISNULL(SUM(CASE WHEN ve.VoucherType_Code IN ('6', 'CV') THEN vs.TotalDebit ELSE 0 END), 0),
        @TotalDebit = ISNULL(SUM(vs.TotalDebit), 0)
    FROM dbo.vw_Acc_VoucherSummary_Fast vs WITH (NOEXPAND)
    JOIN dbo.Acc_VoucherEntry ve WITH (NOLOCK) ON ve.VoucherEntryCodeNo = vs.VoucherEntryCodeNo
    WHERE (ve.IsActive IS NULL OR ve.IsActive != '0')
      AND (@BranchCode IS NULL OR @BranchCode = '' OR ve.BranchCode = @BranchCode)
      AND (@CompanyCode IS NULL OR @CompanyCode = '' OR ve.CompanyCode = @CompanyCode OR ve.Main_CompanyCode = @CompanyCode);

    SELECT 
        @TotalVouchers AS TotalVouchers,
        ISNULL(@VouchersToday, 0) AS VouchersToday,
        ISNULL(@VouchersThisMonth, 0) AS VouchersThisMonth,
        ISNULL(@TotalDebit, 0) AS TotalDebit,
        ISNULL(@TotalDebit, 0) AS TotalCredit,
        (@CrvAmount - @CpvAmount) AS CashBalance,
        (@BrvAmount - @BpvAmount) AS BankBalance,
        ISNULL(@BrvCount, 0) AS BrvCount,
        ISNULL(@BrvAmount, 0) AS BrvAmount,
        ISNULL(@BpvCount, 0) AS BpvCount,
        ISNULL(@BpvAmount, 0) AS BpvAmount,
        ISNULL(@CrvCount, 0) AS CrvCount,
        ISNULL(@CrvAmount, 0) AS CrvAmount,
        ISNULL(@CpvCount, 0) AS CpvCount,
        ISNULL(@CpvAmount, 0) AS CpvAmount,
        ISNULL(@JvCount, 0) AS JvCount,
        ISNULL(@JvAmount, 0) AS JvAmount,
        ISNULL(@CvCount, 0) AS CvCount,
        ISNULL(@CvAmount, 0) AS CvAmount;
END;
GO
