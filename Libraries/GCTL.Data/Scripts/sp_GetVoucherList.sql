SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Helpful Performance Indexes for Large Tables
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Acc_VoucherEntryDetails_CodeNo' AND object_id = OBJECT_ID('Acc_VoucherEntryDetails'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Acc_VoucherEntryDetails_CodeNo 
    ON dbo.Acc_VoucherEntryDetails (VoucherEntryCodeNo) 
    INCLUDE (DebitAmount, CreditAmount, IsActive);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Acc_VoucherEntry_Date' AND object_id = OBJECT_ID('Acc_VoucherEntry'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Acc_VoucherEntry_Date 
    ON dbo.Acc_VoucherEntry (VoucherDate DESC, autoId DESC) 
    INCLUDE (VoucherEntryCodeNo, VoucherNo, VoucherType_Code, BranchCode, IsApproved, IsActive);
END
GO

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
    @SortOrder NVARCHAR(10) = 'DESC'
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize < 1 SET @PageSize = 10;

    DECLARE @Search NVARCHAR(102) = NULL;
    IF @SearchTerm IS NOT NULL AND RTRIM(LTRIM(@SearchTerm)) <> ''
        SET @Search = '%' + RTRIM(LTRIM(@SearchTerm)) + '%';

    -- 1. Paged Result
    ;WITH BaseFilter AS (
        SELECT 
            ve.autoId,
            ve.VoucherEntryCodeNo,
            ve.VoucherNo,
            ve.VoucherType_Code,
            ISNULL(vt.Voucher_TypeName, ve.VoucherType_Code) AS VoucherTypeName,
            ve.VoucherDate,
            ve.FinancialYear,
            ve.BranchCode,
            ISNULL(cb.BranchName, 'Head Office') AS BranchName,
            ve.CompanyCode,
            ve.Main_CompanyCode,
            ve.Narration,
            ve.Reference,
            ve.InvoiceNo,
            ISNULL(ve.IsApproved, 'Pending Approval') AS IsApproved
        FROM Acc_VoucherEntry ve WITH (NOLOCK)
        LEFT JOIN Acc_VoucherType vt WITH (NOLOCK) ON vt.VoucherType_Code = ve.VoucherType_Code
        LEFT JOIN Core_Branch cb WITH (NOLOCK) ON cb.BranchCode = ve.BranchCode
        WHERE (ve.IsActive IS NULL OR ve.IsActive != '0')
          AND (@FromDate IS NULL OR CAST(ve.VoucherDate AS DATE) >= @FromDate)
          AND (@ToDate IS NULL OR CAST(ve.VoucherDate AS DATE) <= @ToDate)
          AND (@VoucherType IS NULL OR @VoucherType = '' OR 
              ve.VoucherType_Code = @VoucherType OR
              (@VoucherType IN ('1', 'BRV') AND ve.VoucherType_Code IN ('1', 'BRV')) OR
              (@VoucherType IN ('2', 'BPV') AND ve.VoucherType_Code IN ('2', 'BPV')) OR
              (@VoucherType IN ('3', 'CRV') AND ve.VoucherType_Code IN ('3', 'CRV')) OR
              (@VoucherType IN ('4', 'CPV') AND ve.VoucherType_Code IN ('4', 'CPV')) OR
              (@VoucherType IN ('5', 'JV') AND ve.VoucherType_Code IN ('5', 'JV')) OR
              (@VoucherType IN ('6', 'CV') AND ve.VoucherType_Code IN ('6', 'CV')))
          AND (@Status IS NULL OR @Status = '' OR 
              (@Status IN ('Pending Approval', 'Pending', 'N') AND (ve.IsApproved IS NULL OR ve.IsApproved IN ('Pending Approval', 'Pending', 'N', ''))) OR
              (@Status IN ('Posted', 'Approved', 'Y') AND ve.IsApproved IN ('Posted', 'Approved', 'Y')) OR
              ve.IsApproved = @Status)
          AND (@BranchCode IS NULL OR @BranchCode = '' OR ve.BranchCode = @BranchCode)
          AND (@CompanyCode IS NULL OR @CompanyCode = '' OR ve.CompanyCode = @CompanyCode OR ve.Main_CompanyCode = @CompanyCode)
          AND (@Search IS NULL OR (
                ve.VoucherNo LIKE @Search OR 
                ve.Narration LIKE @Search OR 
                ve.Reference LIKE @Search OR 
                ve.InvoiceNo LIKE @Search OR
                ve.VoucherType_Code LIKE @Search OR
                vt.Voucher_TypeName LIKE @Search OR
                cb.BranchName LIKE @Search OR
                ISNULL(ve.IsApproved, 'Pending Approval') LIKE @Search OR
                CONVERT(VARCHAR(10), ve.VoucherDate, 103) LIKE @Search OR
                CONVERT(VARCHAR(10), ve.VoucherDate, 120) LIKE @Search
              ))
    ),
    PagedRows AS (
        SELECT 
            b.*,
            COUNT(*) OVER() AS TotalCount
        FROM BaseFilter b
        ORDER BY 
            CASE WHEN LOWER(@SortColumn) = 'voucherno' AND UPPER(@SortOrder) = 'ASC' THEN b.VoucherNo END ASC,
            CASE WHEN LOWER(@SortColumn) = 'voucherno' AND UPPER(@SortOrder) = 'DESC' THEN b.VoucherNo END DESC,
            CASE WHEN LOWER(@SortColumn) = 'voucherdate' AND UPPER(@SortOrder) = 'ASC' THEN b.VoucherDate END ASC,
            CASE WHEN LOWER(@SortColumn) = 'voucherdate' AND UPPER(@SortOrder) = 'DESC' THEN b.VoucherDate END DESC,
            CASE WHEN LOWER(@SortColumn) = 'vouchertype' AND UPPER(@SortOrder) = 'ASC' THEN b.VoucherTypeName END ASC,
            CASE WHEN LOWER(@SortColumn) = 'vouchertype' AND UPPER(@SortOrder) = 'DESC' THEN b.VoucherTypeName END DESC,
            CASE WHEN LOWER(@SortColumn) IN ('branchname', 'branchcode', 'branch') AND UPPER(@SortOrder) = 'ASC' THEN b.BranchName END ASC,
            CASE WHEN LOWER(@SortColumn) IN ('branchname', 'branchcode', 'branch') AND UPPER(@SortOrder) = 'DESC' THEN b.BranchName END DESC,
            CASE WHEN LOWER(@SortColumn) = 'narration' AND UPPER(@SortOrder) = 'ASC' THEN b.Narration END ASC,
            CASE WHEN LOWER(@SortColumn) = 'narration' AND UPPER(@SortOrder) = 'DESC' THEN b.Narration END DESC,
            CASE WHEN LOWER(@SortColumn) IN ('isapproved', 'status') AND UPPER(@SortOrder) = 'ASC' THEN b.IsApproved END ASC,
            CASE WHEN LOWER(@SortColumn) IN ('isapproved', 'status') AND UPPER(@SortOrder) = 'DESC' THEN b.IsApproved END DESC,
            CASE WHEN UPPER(@SortOrder) = 'ASC' THEN b.VoucherDate END ASC,
            CASE WHEN UPPER(@SortOrder) = 'DESC' THEN b.VoucherDate END DESC,
            b.autoId DESC
        OFFSET (@PageNumber - 1) * @PageSize ROWS
        FETCH NEXT @PageSize ROWS ONLY
    )
    SELECT 
        p.autoId,
        p.VoucherEntryCodeNo,
        p.VoucherNo,
        p.VoucherType_Code,
        p.VoucherTypeName,
        p.VoucherDate,
        p.FinancialYear,
        p.BranchCode,
        p.BranchName,
        p.CompanyCode,
        p.Main_CompanyCode,
        p.Narration,
        p.Reference,
        p.InvoiceNo,
        p.IsApproved,
        ISNULL(dr.TotalAmount, 0) AS TotalAmount,
        p.TotalCount
    FROM PagedRows p
    OUTER APPLY (
        SELECT SUM(ISNULL(ved.DebitAmount, 0)) AS TotalAmount
        FROM Acc_VoucherEntryDetails ved WITH (NOLOCK)
        WHERE ved.VoucherEntryCodeNo = p.VoucherEntryCodeNo AND (ved.IsActive IS NULL OR ved.IsActive != '0')
    ) dr
    ORDER BY 
        CASE WHEN LOWER(@SortColumn) = 'voucherno' AND UPPER(@SortOrder) = 'ASC' THEN p.VoucherNo END ASC,
        CASE WHEN LOWER(@SortColumn) = 'voucherno' AND UPPER(@SortOrder) = 'DESC' THEN p.VoucherNo END DESC,
        CASE WHEN LOWER(@SortColumn) = 'voucherdate' AND UPPER(@SortOrder) = 'ASC' THEN p.VoucherDate END ASC,
        CASE WHEN LOWER(@SortColumn) = 'voucherdate' AND UPPER(@SortOrder) = 'DESC' THEN p.VoucherDate END DESC,
        CASE WHEN LOWER(@SortColumn) = 'vouchertype' AND UPPER(@SortOrder) = 'ASC' THEN p.VoucherTypeName END ASC,
        CASE WHEN LOWER(@SortColumn) = 'vouchertype' AND UPPER(@SortOrder) = 'DESC' THEN p.VoucherTypeName END DESC,
        CASE WHEN LOWER(@SortColumn) IN ('branchname', 'branchcode', 'branch') AND UPPER(@SortOrder) = 'ASC' THEN p.BranchName END ASC,
        CASE WHEN LOWER(@SortColumn) IN ('branchname', 'branchcode', 'branch') AND UPPER(@SortOrder) = 'DESC' THEN p.BranchName END DESC,
        CASE WHEN LOWER(@SortColumn) = 'narration' AND UPPER(@SortOrder) = 'ASC' THEN p.Narration END ASC,
        CASE WHEN LOWER(@SortColumn) = 'narration' AND UPPER(@SortOrder) = 'DESC' THEN p.Narration END DESC,
        CASE WHEN LOWER(@SortColumn) IN ('isapproved', 'status') AND UPPER(@SortOrder) = 'ASC' THEN p.IsApproved END ASC,
        CASE WHEN LOWER(@SortColumn) IN ('isapproved', 'status') AND UPPER(@SortOrder) = 'DESC' THEN p.IsApproved END DESC,
        p.autoId DESC;

    -- 2. KPI Summary
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

    SELECT 
        @TotalVouchers = COUNT(*),
        @VouchersToday = SUM(CASE WHEN CAST(ve.VoucherDate AS DATE) = @Today THEN 1 ELSE 0 END),
        @VouchersThisMonth = SUM(CASE WHEN CAST(ve.VoucherDate AS DATE) >= @MonthStart AND CAST(ve.VoucherDate AS DATE) <= @MonthEnd THEN 1 ELSE 0 END),
        @BrvCount = SUM(CASE WHEN ve.VoucherType_Code IN ('1', 'BRV') THEN 1 ELSE 0 END),
        @BpvCount = SUM(CASE WHEN ve.VoucherType_Code IN ('2', 'BPV') THEN 1 ELSE 0 END),
        @CrvCount = SUM(CASE WHEN ve.VoucherType_Code IN ('3', 'CRV') THEN 1 ELSE 0 END),
        @CpvCount = SUM(CASE WHEN ve.VoucherType_Code IN ('4', 'CPV') THEN 1 ELSE 0 END),
        @JvCount = SUM(CASE WHEN ve.VoucherType_Code IN ('5', 'JV') THEN 1 ELSE 0 END),
        @CvCount = SUM(CASE WHEN ve.VoucherType_Code IN ('6', 'CV') THEN 1 ELSE 0 END)
    FROM Acc_VoucherEntry ve WITH (NOLOCK)
    WHERE (ve.IsActive IS NULL OR ve.IsActive != '0')
      AND (@BranchCode IS NULL OR @BranchCode = '' OR ve.BranchCode = @BranchCode)
      AND (@CompanyCode IS NULL OR @CompanyCode = '' OR ve.CompanyCode = @CompanyCode OR ve.Main_CompanyCode = @CompanyCode);

    SELECT 
        @BrvAmount = ISNULL(SUM(CASE WHEN ve.VoucherType_Code IN ('1', 'BRV') THEN ved.DebitAmount ELSE 0 END), 0),
        @BpvAmount = ISNULL(SUM(CASE WHEN ve.VoucherType_Code IN ('2', 'BPV') THEN ved.CreditAmount ELSE 0 END), 0),
        @CrvAmount = ISNULL(SUM(CASE WHEN ve.VoucherType_Code IN ('3', 'CRV') THEN ved.DebitAmount ELSE 0 END), 0),
        @CpvAmount = ISNULL(SUM(CASE WHEN ve.VoucherType_Code IN ('4', 'CPV') THEN ved.CreditAmount ELSE 0 END), 0),
        @JvAmount = ISNULL(SUM(CASE WHEN ve.VoucherType_Code IN ('5', 'JV') THEN ved.DebitAmount ELSE 0 END), 0),
        @CvAmount = ISNULL(SUM(CASE WHEN ve.VoucherType_Code IN ('6', 'CV') THEN ved.DebitAmount ELSE 0 END), 0),
        @TotalDebit = ISNULL(SUM(ved.DebitAmount), 0)
    FROM Acc_VoucherEntryDetails ved WITH (NOLOCK)
    JOIN Acc_VoucherEntry ve WITH (NOLOCK) ON ve.VoucherEntryCodeNo = ved.VoucherEntryCodeNo
    WHERE (ved.IsActive IS NULL OR ved.IsActive != '0')
      AND (ve.IsActive IS NULL OR ve.IsActive != '0')
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
