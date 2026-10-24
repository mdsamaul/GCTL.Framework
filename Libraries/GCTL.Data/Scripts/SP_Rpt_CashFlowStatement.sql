
CREATE PROCEDURE [dbo].[SP_Rpt_CashFlowStatement]
    @Main_CompanyCode NVARCHAR(50) = NULL,
    @BranchCode NVARCHAR(50) = NULL,
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Default dates if not supplied
    IF @FromDate IS NULL
        SET @FromDate = DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE()), 0); -- First day of current month
    IF @ToDate IS NULL
        SET @ToDate = GETDATE();

    -- Treat empty, '0', or 'All' as wildcard
    IF @Main_CompanyCode = '' OR @Main_CompanyCode = '0' SET @Main_CompanyCode = NULL;
    IF @BranchCode = '' OR @BranchCode = 'All' OR @BranchCode = '0' SET @BranchCode = NULL;

    DECLARE @FromDateOnly DATE = CAST(@FromDate AS DATE);
    DECLARE @ToDateOnly DATE = CAST(@ToDate AS DATE);

    ------------------------------------------------------------------------------------------
    -- 1. Identify Cash & Bank Ledgers (SubControl 102002 or SubSubsidiary starting with 102002)
    ------------------------------------------------------------------------------------------
    CREATE TABLE #CashBankLedgers (
        GeneralLedgerCodeNo NVARCHAR(50) PRIMARY KEY
    );

    INSERT INTO #CashBankLedgers (GeneralLedgerCodeNo)
    SELECT gl.GeneralLedgerCodeNo
    FROM dbo.Acc_GeneralLedger gl
    WHERE gl.SubControlLedgerCodeNo = '102002' 
       OR gl.SubsidiaryLedgerCodeNo LIKE '102002%';

    ------------------------------------------------------------------------------------------
    -- 2. Calculate Opening Cash & Bank Balance before @FromDate
    ------------------------------------------------------------------------------------------
    -- 2.1 From Opening Balance Setup Table
    DECLARE @TotalOB DECIMAL(18, 2) = 0;
    SELECT @TotalOB = ISNULL(SUM(
        CASE 
            WHEN ob.TrType IN ('Debit', 'Dr', 'DR') THEN ISNULL(ob.OpeningBalance, 0)
            ELSE -ISNULL(ob.OpeningBalance, 0)
        END
    ), 0)
    FROM dbo.Acc_Company_Opening_Balance ob
    INNER JOIN #CashBankLedgers cbl ON ob.GeneralLedgerCodeNo = cbl.GeneralLedgerCodeNo
    WHERE ob.DeletedAt IS NULL
      AND (@Main_CompanyCode IS NULL OR CAST(ob.Main_CompanyCode AS NVARCHAR(50)) = @Main_CompanyCode)
      AND (@BranchCode IS NULL OR CAST(ob.BranchCode AS NVARCHAR(50)) = @BranchCode);

    -- 2.2 From Posted Vouchers prior to @FromDate
    DECLARE @PriorNetMovement DECIMAL(18, 2) = 0;
    SELECT @PriorNetMovement = ISNULL(SUM(ISNULL(vd.DebitAmount, 0) - ISNULL(vd.CreditAmount, 0)), 0)
    FROM dbo.Acc_VoucherEntry v
    INNER JOIN dbo.Acc_VoucherEntryDetails vd ON v.VoucherEntryCodeNo = vd.VoucherEntryCodeNo
    INNER JOIN #CashBankLedgers cbl ON vd.AccCode = cbl.GeneralLedgerCodeNo
    WHERE CAST(v.VoucherDate AS DATE) < @FromDateOnly
      AND v.IsApproved = 'Posted'
      AND ISNULL(v.IsActive, '1') != '0'
      AND (@Main_CompanyCode IS NULL OR v.Main_CompanyCode = @Main_CompanyCode)
      AND (@BranchCode IS NULL OR v.BranchCode = @BranchCode);

    DECLARE @OpeningCash DECIMAL(18, 2) = @TotalOB + @PriorNetMovement;

    ------------------------------------------------------------------------------------------
    -- 3. Identify Posted Vouchers involving Cash & Bank in the specified period
    ------------------------------------------------------------------------------------------
    CREATE TABLE #CashInvolvingVouchers (
        VoucherEntryCodeNo NVARCHAR(50) PRIMARY KEY
    );

    INSERT INTO #CashInvolvingVouchers (VoucherEntryCodeNo)
    SELECT DISTINCT v.VoucherEntryCodeNo
    FROM dbo.Acc_VoucherEntry v
    INNER JOIN dbo.Acc_VoucherEntryDetails vd ON v.VoucherEntryCodeNo = vd.VoucherEntryCodeNo
    INNER JOIN #CashBankLedgers cbl ON vd.AccCode = cbl.GeneralLedgerCodeNo
    WHERE CAST(v.VoucherDate AS DATE) >= @FromDateOnly 
      AND CAST(v.VoucherDate AS DATE) <= @ToDateOnly
      AND v.IsApproved = 'Posted'
      AND ISNULL(v.IsActive, '1') != '0'
      AND (@Main_CompanyCode IS NULL OR v.Main_CompanyCode = @Main_CompanyCode)
      AND (@BranchCode IS NULL OR v.BranchCode = @BranchCode);

    ------------------------------------------------------------------------------------------
    -- 4. Calculate Net Cash Impact for Non-Cash Accounts and Categorize
    --    In prototype: cat[c][g.id] = (cat[c][g.id] || 0) + l.cr - l.dr
    --    c = g.t > '2' || ['102','203'].includes(layer2) ? 'O' : layer2 === '101' ? 'I' : 'F'
    ------------------------------------------------------------------------------------------
    CREATE TABLE #GroupedActivityItems (
        ActivityType NVARCHAR(50),
        ActivityOrder INT,
        GeneralLedgerCodeNo NVARCHAR(50),
        GeneralLedgerName NVARCHAR(200),
        SubSubsidiaryLedgerName NVARCHAR(200),
        Receipts DECIMAL(18, 2),
        Payments DECIMAL(18, 2),
        NetAmount DECIMAL(18, 2),
        DisplayLabel NVARCHAR(300)
    );

    INSERT INTO #GroupedActivityItems (
        ActivityType,
        ActivityOrder,
        GeneralLedgerCodeNo,
        GeneralLedgerName,
        SubSubsidiaryLedgerName,
        Receipts,
        Payments,
        NetAmount,
        DisplayLabel
    )
    SELECT 
        CASE 
            -- Investing: 101 Non-Current Assets (PPE, Intangibles)
            WHEN gl.ControlLedgerCodeNo = '101' THEN 'Investing'

            -- Financing: 201 Equity, 202 Non-Current Liabilities (Loans)
            WHEN gl.ControlLedgerCodeNo IN ('201', '202') THEN 'Financing'

            -- Operating: 3 (Income), 4 (Expense), 102 (Current Assets excl Cash), 203 (Current Liabilities)
            ELSE 'Operating'
        END AS ActivityType,
        CASE 
            WHEN gl.ControlLedgerCodeNo = '101' THEN 2
            WHEN gl.ControlLedgerCodeNo IN ('201', '202') THEN 3
            ELSE 1
        END AS ActivityOrder,
        vd.AccCode AS GeneralLedgerCodeNo,
        gl.GeneralLedgerName,
        ISNULL(ssl.SubSubsidiaryLedgerName, 'General Ledger') AS SubSubsidiaryLedgerName,
        CASE 
            WHEN SUM(ISNULL(vd.CreditAmount, 0) - ISNULL(vd.DebitAmount, 0)) >= 0 
                THEN SUM(ISNULL(vd.CreditAmount, 0) - ISNULL(vd.DebitAmount, 0))
            ELSE 0 
        END AS Receipts,
        CASE 
            WHEN SUM(ISNULL(vd.CreditAmount, 0) - ISNULL(vd.DebitAmount, 0)) < 0 
                THEN ABS(SUM(ISNULL(vd.CreditAmount, 0) - ISNULL(vd.DebitAmount, 0)))
            ELSE 0 
        END AS Payments,
        SUM(ISNULL(vd.CreditAmount, 0) - ISNULL(vd.DebitAmount, 0)) AS NetAmount,
        CASE 
            WHEN SUM(ISNULL(vd.CreditAmount, 0) - ISNULL(vd.DebitAmount, 0)) >= 0 
                THEN 'Receipts from ' + gl.GeneralLedgerName
            ELSE 'Payments for ' + gl.GeneralLedgerName
        END AS DisplayLabel
    FROM dbo.Acc_VoucherEntryDetails vd
    INNER JOIN #CashInvolvingVouchers civ ON vd.VoucherEntryCodeNo = civ.VoucherEntryCodeNo
    LEFT JOIN #CashBankLedgers cbl ON vd.AccCode = cbl.GeneralLedgerCodeNo
    INNER JOIN dbo.Acc_GeneralLedger gl ON vd.AccCode = gl.GeneralLedgerCodeNo
    LEFT JOIN dbo.Acc_SubSubsidiaryLedger ssl ON gl.SubsidiaryLedgerCodeNo = ssl.SubsidiaryLedgerCodeNo
    WHERE cbl.GeneralLedgerCodeNo IS NULL -- Skip Cash/Bank accounts themselves
    GROUP BY 
        gl.ControlLedgerCodeNo,
        gl.GroupLedgerCodeNo,
        vd.AccCode,
        gl.GeneralLedgerName,
        ssl.SubSubsidiaryLedgerName
    HAVING ABS(SUM(ISNULL(vd.CreditAmount, 0) - ISNULL(vd.DebitAmount, 0))) >= 0.005;

    -- Period Cash Movement on Cash/Bank accounts directly
    DECLARE @PeriodNetMovement DECIMAL(18, 2) = 0;
    SELECT @PeriodNetMovement = ISNULL(SUM(ISNULL(vd.DebitAmount, 0) - ISNULL(vd.CreditAmount, 0)), 0)
    FROM dbo.Acc_VoucherEntry v
    INNER JOIN dbo.Acc_VoucherEntryDetails vd ON v.VoucherEntryCodeNo = vd.VoucherEntryCodeNo
    INNER JOIN #CashBankLedgers cbl ON vd.AccCode = cbl.GeneralLedgerCodeNo
    WHERE CAST(v.VoucherDate AS DATE) >= @FromDateOnly 
      AND CAST(v.VoucherDate AS DATE) <= @ToDateOnly
      AND v.IsApproved = 'Posted'
      AND ISNULL(v.IsActive, '1') != '0'
      AND (@Main_CompanyCode IS NULL OR v.Main_CompanyCode = @Main_CompanyCode)
      AND (@BranchCode IS NULL OR v.BranchCode = @BranchCode);

    ------------------------------------------------------------------------------------------
    -- 5. Return Result Set 1: Detail Lines for Activities
    ------------------------------------------------------------------------------------------
    SELECT 
        ActivityType,
        ActivityOrder,
        GeneralLedgerCodeNo,
        GeneralLedgerName,
        SubSubsidiaryLedgerName,
        Receipts AS TotalReceipts,
        Payments AS TotalPayments,
        NetAmount,
        DisplayLabel
    FROM #GroupedActivityItems
    ORDER BY ActivityOrder, NetAmount DESC, GeneralLedgerName;

    ------------------------------------------------------------------------------------------
    -- 6. Return Result Set 2: Summary Reconciliation
    ------------------------------------------------------------------------------------------
    DECLARE @OperatingFlow DECIMAL(18, 2) = ISNULL((SELECT SUM(NetAmount) FROM #GroupedActivityItems WHERE ActivityType = 'Operating'), 0);
    DECLARE @InvestingFlow DECIMAL(18, 2) = ISNULL((SELECT SUM(NetAmount) FROM #GroupedActivityItems WHERE ActivityType = 'Investing'), 0);
    DECLARE @FinancingFlow DECIMAL(18, 2) = ISNULL((SELECT SUM(NetAmount) FROM #GroupedActivityItems WHERE ActivityType = 'Financing'), 0);
    DECLARE @NetFlow DECIMAL(18, 2) = @OperatingFlow + @InvestingFlow + @FinancingFlow;
    DECLARE @ClosingCash DECIMAL(18, 2) = @OpeningCash + @PeriodNetMovement;
    DECLARE @Diff DECIMAL(18, 2) = @ClosingCash - (@OpeningCash + @NetFlow);

    SELECT 
        @OpeningCash AS OpeningCashAndBank,
        @OperatingFlow AS OperatingCashFlow,
        @InvestingFlow AS InvestingCashFlow,
        @FinancingFlow AS FinancingCashFlow,
        @NetFlow AS NetCashFlow,
        @ClosingCash AS ClosingCashAndBank,
        @Diff AS Difference,
        CASE WHEN ABS(@Diff) < 0.01 THEN 1 ELSE 0 END AS IsReconciled;

    -- Cleanup
    DROP TABLE #CashBankLedgers;
    DROP TABLE #CashInvolvingVouchers;
    DROP TABLE #GroupedActivityItems;
END
