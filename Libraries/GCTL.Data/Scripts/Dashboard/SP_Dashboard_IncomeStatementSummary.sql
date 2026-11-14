CREATE OR ALTER PROCEDURE dbo.SP_Dashboard_IncomeStatementSummary
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @BranchCode NVARCHAR(50) = '',
    @Main_CompanyCode NVARCHAR(50) = ''
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -3, CAST(GETDATE() AS DATE));
    IF @ToDate IS NULL SET @ToDate = CAST(GETDATE() AS DATE);
    SET @BranchCode = LTRIM(RTRIM(ISNULL(@BranchCode, '')));
    IF @BranchCode = 'All' SET @BranchCode = '';
    SET @Main_CompanyCode = LTRIM(RTRIM(ISNULL(@Main_CompanyCode, '')));
    IF @Main_CompanyCode = '' SET @Main_CompanyCode = '001';

    DECLARE @FromDateD  DATE = CAST(@FromDate AS DATE);
    DECLARE @ToDateExcl DATE = DATEADD(DAY, 1, CAST(@ToDate AS DATE));

    ------------------------------------------------------------------
    -- 1. Period Vouchers for Group 3 (Expenses) and Group 4 (Revenue)
    ------------------------------------------------------------------
    CREATE TABLE #Vouchers
    (
        GeneralLedgerCodeNo NVARCHAR(50) NOT NULL PRIMARY KEY,
        PerDr NUMERIC(18,2) NOT NULL,
        PerCr NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #Vouchers (GeneralLedgerCodeNo, PerDr, PerCr)
    SELECT
        gl.GeneralLedgerCodeNo,
        SUM(CASE WHEN ved.TrType = 'Dr' THEN ISNULL(ved.DebitAmount, 0)  ELSE 0 END),
        SUM(CASE WHEN ved.TrType = 'Cr' THEN ISNULL(ved.CreditAmount, 0) ELSE 0 END)
    FROM dbo.Acc_VoucherEntry ve
    INNER JOIN dbo.Acc_VoucherEntryDetails ved
        ON ved.VoucherEntryCodeNo = ve.VoucherEntryCodeNo
    INNER JOIN dbo.Acc_GeneralLedger gl
        ON gl.GeneralLedgerCodeNo = ved.AccCode
    WHERE (ve.IsApproved IN ('Y', 'Posted', '1') OR ve.IsApproved IS NULL)
      AND (ve.IsActive IS NULL OR ve.IsActive = '1')
      AND ve.VoucherDate >= @FromDateD
      AND ve.VoucherDate <  @ToDateExcl
      AND (@Main_CompanyCode = '' OR ve.Main_CompanyCode = @Main_CompanyCode OR ve.CompanyCode = @Main_CompanyCode)
      AND (@BranchCode = ''       OR ve.BranchCode       = @BranchCode)
      AND (gl.GroupLedgerCodeNo IN ('3', '4') OR gl.GroupLedgerCodeNo LIKE '3%' OR gl.GroupLedgerCodeNo LIKE '4%')
    GROUP BY gl.GeneralLedgerCodeNo
    OPTION (RECOMPILE);

    ------------------------------------------------------------------
    -- 2. Leaf (General Ledger) Level
    ------------------------------------------------------------------
    CREATE TABLE #Leaf
    (
        Section     NVARCHAR(20)  NOT NULL, -- 'Revenue' or 'Expense'
        GroupCode   NVARCHAR(50)  NOT NULL,
        GroupName   NVARCHAR(250) NULL,
        ControlCode NVARCHAR(50)  NOT NULL,
        ControlName NVARCHAR(250) NULL,
        SubCode     NVARCHAR(50)  NOT NULL,
        SubName     NVARCHAR(250) NULL,
        GLCode      NVARCHAR(50)  NOT NULL,
        GLName      NVARCHAR(250) NULL,
        PerDr       NUMERIC(18,2) NOT NULL,
        PerCr       NUMERIC(18,2) NOT NULL,
        Amount      NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #Leaf
        (Section, GroupCode, GroupName, ControlCode, ControlName, SubCode, SubName,
         GLCode, GLName, PerDr, PerCr, Amount)
    SELECT
        CASE WHEN g.GroupLedgerCodeNo = '4' OR g.GroupLedgerCodeNo LIKE '4%' THEN 'Revenue' ELSE 'Expense' END,
        ISNULL(g.GroupLedgerCodeNo, ''),
        ISNULL(grp.GroupLedgerName, N'(Unassigned)'),
        ISNULL(g.ControlLedgerCodeNo, ''),
        ISNULL(ctl.ControlLedgerName, N'Other'),
        ISNULL(g.SubControlLedgerCodeNo, ''),
        ISNULL(sc.SubControlLedgerName, N'General'),
        g.GeneralLedgerCodeNo,
        g.GeneralLedgerName,
        ISNULL(v.PerDr, 0),
        ISNULL(v.PerCr, 0),
        CASE 
            WHEN g.GroupLedgerCodeNo = '4' OR g.GroupLedgerCodeNo LIKE '4%' 
                THEN ISNULL(v.PerCr, 0) - ISNULL(v.PerDr, 0)
            ELSE 
                ISNULL(v.PerDr, 0) - ISNULL(v.PerCr, 0)
        END
    FROM dbo.Acc_GeneralLedger g
    LEFT JOIN dbo.Acc_GroupLedger grp
        ON grp.GroupLedgerCodeNo = g.GroupLedgerCodeNo
    LEFT JOIN dbo.Acc_ControlLedger ctl
        ON ctl.GroupLedgerCodeNo   = g.GroupLedgerCodeNo
       AND ctl.ControlLedgerCodeNo = g.ControlLedgerCodeNo
    LEFT JOIN dbo.Acc_SubControlLedger sc
        ON sc.GroupLedgerCodeNo      = g.GroupLedgerCodeNo
       AND sc.ControlLedgerCodeNo    = g.ControlLedgerCodeNo
       AND sc.SubControlLedgerCodeNo = g.SubControlLedgerCodeNo
    INNER JOIN #Vouchers v 
        ON v.GeneralLedgerCodeNo = g.GeneralLedgerCodeNo
    WHERE (g.IsActive IS NULL OR g.IsActive = '1')
      AND (
          (g.GroupLedgerCodeNo IN ('3', '4')) OR 
          (g.GroupLedgerCodeNo LIKE '3%' OR g.GroupLedgerCodeNo LIKE '4%')
      );

    ------------------------------------------------------------------
    -- Calculations for KPIs
    ------------------------------------------------------------------
    DECLARE @TotalRev NUMERIC(18,2) = ISNULL((SELECT SUM(Amount) FROM #Leaf WHERE Section = 'Revenue'), 0);
    DECLARE @OpRev NUMERIC(18,2) = ISNULL((SELECT SUM(Amount) FROM #Leaf WHERE Section = 'Revenue' AND (ControlCode = '401' OR ControlCode LIKE '401%')), 0);
    IF @OpRev = 0 SET @OpRev = @TotalRev;
    DECLARE @NonOpRev NUMERIC(18,2) = @TotalRev - @OpRev;

    -- Distinct breakdown by Control Code:
    -- 301: Direct Expenses
    -- 302: Indirect Expenses
    -- 303: Finance Cost
    -- 304: Income Tax
    DECLARE @DirectExp NUMERIC(18,2) = ISNULL((SELECT SUM(Amount) FROM #Leaf WHERE Section = 'Expense' AND (ControlCode = '301' OR ControlCode LIKE '301%' OR ControlCode = '305' OR ControlCode LIKE '305%')), 0);
    DECLARE @IndirectExp NUMERIC(18,2) = ISNULL((SELECT SUM(Amount) FROM #Leaf WHERE Section = 'Expense' AND (ControlCode = '302' OR ControlCode LIKE '302%')), 0);
    DECLARE @FinCost NUMERIC(18,2) = ISNULL((SELECT SUM(Amount) FROM #Leaf WHERE Section = 'Expense' AND (ControlCode = '303' OR ControlCode LIKE '303%')), 0);
    DECLARE @TaxExp NUMERIC(18,2) = ISNULL((SELECT SUM(Amount) FROM #Leaf WHERE Section = 'Expense' AND (ControlCode = '304' OR ControlCode LIKE '304%')), 0);

    DECLARE @TotalExp NUMERIC(18,2) = ISNULL((SELECT SUM(Amount) FROM #Leaf WHERE Section = 'Expense'), 0);
    IF @DirectExp = 0 AND @IndirectExp = 0 AND @TotalExp > 0
    BEGIN
        SET @DirectExp = @TotalExp;
    END

    DECLARE @GrossProfit NUMERIC(18,2) = @OpRev - @DirectExp;
    DECLARE @NetProfit NUMERIC(18,2) = @TotalRev - @TotalExp;

    ---------------------------------------------------------------------
    -- RESULT SET 1: KPIs
    ---------------------------------------------------------------------
    SELECT 
        @TotalRev AS TotalRevenue,
        @OpRev AS OperatingRevenue,
        @NonOpRev AS NonOperatingRevenue,
        @DirectExp AS DirectExpenses,
        @GrossProfit AS GrossProfit,
        @IndirectExp AS IndirectExpenses,
        @FinCost AS FinanceCost,
        @TaxExp AS TaxExpense,
        @NetProfit AS NetProfit,
        CASE WHEN @OpRev <> 0 THEN ROUND((@GrossProfit / @OpRev) * 100.0, 2) ELSE 0 END AS GPMarginPct,
        CASE WHEN @TotalRev <> 0 THEN ROUND((@NetProfit / @TotalRev) * 100.0, 2) ELSE 0 END AS NPMarginPct,
        CASE WHEN @OpRev <> 0 THEN ROUND((@DirectExp / @OpRev) * 100.0, 1) ELSE 0 END AS DirectPctOfRevenue;

    ---------------------------------------------------------------------
    -- RESULT SET 2: Expense Composition (Top SubControl Heads)
    ---------------------------------------------------------------------
    SELECT TOP 7
        LTRIM(RTRIM(SubName)) AS Label,
        SUM(Amount) AS Amount
    FROM #Leaf
    WHERE Section = 'Expense' AND Amount > 0
    GROUP BY SubCode, SubName
    ORDER BY SUM(Amount) DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 3: Statement Grid Rows (Matching CNF_Dashboard.html)
    ---------------------------------------------------------------------
    CREATE TABLE #Rows
    (
        SortOrder INT IDENTITY(1,1),
        Label NVARCHAR(300),
        Amt NUMERIC(18,2) NULL,
        Kind NVARCHAR(20) -- 'head', 'item', 'sub', 'total'
    );

    -- 3A. REVENUE
    INSERT INTO #Rows (Label, Amt, Kind) VALUES ('REVENUE', NULL, 'head');

    -- Operating Revenue Items (401)
    INSERT INTO #Rows (Label, Amt, Kind)
    SELECT 
        SubCode + ' – ' + SubName AS Label,
        SUM(Amount) AS Amt,
        'item' AS Kind
    FROM #Leaf
    WHERE Section = 'Revenue' AND (ControlCode = '401' OR ControlCode LIKE '401%')
    GROUP BY SubCode, SubName
    HAVING SUM(Amount) <> 0
    ORDER BY SubCode;

    -- Operating Income subtotal
    INSERT INTO #Rows (Label, Amt, Kind) VALUES ('Operating Income', @OpRev, 'sub');

    -- Non-operating revenue items (402 or others)
    INSERT INTO #Rows (Label, Amt, Kind)
    SELECT 
        SubCode + ' – ' + SubName AS Label,
        SUM(Amount) AS Amt,
        'item' AS Kind
    FROM #Leaf
    WHERE Section = 'Revenue' AND NOT (ControlCode = '401' OR ControlCode LIKE '401%')
    GROUP BY SubCode, SubName
    HAVING SUM(Amount) <> 0
    ORDER BY SubCode;

    INSERT INTO #Rows (Label, Amt, Kind) VALUES ('Total Revenue', @TotalRev, 'total');

    -- 3B. DIRECT EXPENSES (301)
    INSERT INTO #Rows (Label, Amt, Kind) VALUES ('DIRECT EXPENSES', NULL, 'head');

    INSERT INTO #Rows (Label, Amt, Kind)
    SELECT 
        SubCode + ' – ' + SubName AS Label,
        SUM(Amount) AS Amt,
        'item' AS Kind
    FROM #Leaf
    WHERE Section = 'Expense' AND (ControlCode = '301' OR ControlCode LIKE '301%' OR ControlCode = '305' OR ControlCode LIKE '305%')
    GROUP BY SubCode, SubName
    HAVING SUM(Amount) <> 0
    ORDER BY SubCode;

    INSERT INTO #Rows (Label, Amt, Kind) VALUES ('Total Direct Expenses', @DirectExp, 'sub');
    INSERT INTO #Rows (Label, Amt, Kind) VALUES ('GROSS PROFIT', @GrossProfit, 'total');

    -- 3C. INDIRECT EXPENSES (302)
    IF EXISTS (SELECT 1 FROM #Leaf WHERE Section = 'Expense' AND (ControlCode = '302' OR ControlCode LIKE '302%'))
    BEGIN
        INSERT INTO #Rows (Label, Amt, Kind) VALUES ('INDIRECT EXPENSES', NULL, 'head');

        INSERT INTO #Rows (Label, Amt, Kind)
        SELECT 
            SubCode + ' – ' + SubName AS Label,
            SUM(Amount) AS Amt,
            'item' AS Kind
        FROM #Leaf
        WHERE Section = 'Expense' AND (ControlCode = '302' OR ControlCode LIKE '302%')
        GROUP BY SubCode, SubName
        HAVING SUM(Amount) <> 0
        ORDER BY SubCode;

        INSERT INTO #Rows (Label, Amt, Kind) VALUES ('Total Indirect Expenses', @IndirectExp, 'sub');
    END

    -- 3D. FINANCE COST (303)
    IF EXISTS (SELECT 1 FROM #Leaf WHERE Section = 'Expense' AND (ControlCode = '303' OR ControlCode LIKE '303%'))
    BEGIN
        INSERT INTO #Rows (Label, Amt, Kind) VALUES ('FINANCE COST', NULL, 'head');

        INSERT INTO #Rows (Label, Amt, Kind)
        SELECT 
            SubCode + ' – ' + SubName AS Label,
            SUM(Amount) AS Amt,
            'item' AS Kind
        FROM #Leaf
        WHERE Section = 'Expense' AND (ControlCode = '303' OR ControlCode LIKE '303%')
        GROUP BY SubCode, SubName
        HAVING SUM(Amount) <> 0
        ORDER BY SubCode;

        INSERT INTO #Rows (Label, Amt, Kind) VALUES ('Total Finance Cost', @FinCost, 'sub');
    END

    DECLARE @PBT NUMERIC(18,2) = @TotalRev - @DirectExp - @IndirectExp - @FinCost;
    INSERT INTO #Rows (Label, Amt, Kind) VALUES ('PROFIT BEFORE TAX', @PBT, 'total');

    -- 3E. INCOME TAX (304)
    IF EXISTS (SELECT 1 FROM #Leaf WHERE Section = 'Expense' AND (ControlCode = '304' OR ControlCode LIKE '304%'))
    BEGIN
        INSERT INTO #Rows (Label, Amt, Kind) VALUES ('INCOME TAX', NULL, 'head');

        INSERT INTO #Rows (Label, Amt, Kind)
        SELECT 
            SubCode + ' – ' + SubName AS Label,
            SUM(Amount) AS Amt,
            'item' AS Kind
        FROM #Leaf
        WHERE Section = 'Expense' AND (ControlCode = '304' OR ControlCode LIKE '304%')
        GROUP BY SubCode, SubName
        HAVING SUM(Amount) <> 0
        ORDER BY SubCode;

        INSERT INTO #Rows (Label, Amt, Kind) VALUES ('Total Income Tax', @TaxExp, 'sub');
    END

    INSERT INTO #Rows (Label, Amt, Kind) VALUES ('NET PROFIT / (LOSS)', @NetProfit, 'total');

    SELECT 
        Label,
        Amt,
        CASE WHEN @TotalRev <> 0 AND Amt IS NOT NULL THEN ROUND((Amt / @TotalRev) * 100.0, 2) ELSE NULL END AS Pct,
        Kind
    FROM #Rows
    ORDER BY SortOrder;

    ---------------------------------------------------------------------
    -- RESULT SET 4: Monthly Trends (Revenue vs Expense vs Net)
    ---------------------------------------------------------------------
    SELECT 
        FORMAT(ve.VoucherDate, 'yyyy-MM') AS MonthKey,
        FORMAT(ve.VoucherDate, 'MMM yy') AS MonthLabel,
        SUM(CASE WHEN (gl.GroupLedgerCodeNo IN ('4') OR gl.GroupLedgerCodeNo LIKE '4%') AND ved.TrType = 'Cr' THEN ISNULL(ved.CreditAmount, 0)
                 WHEN (gl.GroupLedgerCodeNo IN ('4') OR gl.GroupLedgerCodeNo LIKE '4%') AND ved.TrType = 'Dr' THEN -ISNULL(ved.DebitAmount, 0)
                 ELSE 0 END) AS Revenue,
        SUM(CASE WHEN (gl.GroupLedgerCodeNo IN ('3') OR gl.GroupLedgerCodeNo LIKE '3%') AND ved.TrType = 'Dr' THEN ISNULL(ved.DebitAmount, 0)
                 WHEN (gl.GroupLedgerCodeNo IN ('3') OR gl.GroupLedgerCodeNo LIKE '3%') AND ved.TrType = 'Cr' THEN -ISNULL(ved.CreditAmount, 0)
                 ELSE 0 END) AS Expense,
        SUM(CASE WHEN (gl.GroupLedgerCodeNo IN ('4') OR gl.GroupLedgerCodeNo LIKE '4%') AND ved.TrType = 'Cr' THEN ISNULL(ved.CreditAmount, 0)
                 WHEN (gl.GroupLedgerCodeNo IN ('4') OR gl.GroupLedgerCodeNo LIKE '4%') AND ved.TrType = 'Dr' THEN -ISNULL(ved.DebitAmount, 0)
                 ELSE 0 END) -
        SUM(CASE WHEN (gl.GroupLedgerCodeNo IN ('3') OR gl.GroupLedgerCodeNo LIKE '3%') AND ved.TrType = 'Dr' THEN ISNULL(ved.DebitAmount, 0)
                 WHEN (gl.GroupLedgerCodeNo IN ('3') OR gl.GroupLedgerCodeNo LIKE '3%') AND ved.TrType = 'Cr' THEN -ISNULL(ved.CreditAmount, 0)
                 ELSE 0 END) AS NetProfit
    FROM dbo.Acc_VoucherEntry ve
    INNER JOIN dbo.Acc_VoucherEntryDetails ved ON ved.VoucherEntryCodeNo = ve.VoucherEntryCodeNo
    INNER JOIN dbo.Acc_GeneralLedger gl ON gl.GeneralLedgerCodeNo = ved.AccCode
    WHERE (ve.IsApproved IN ('Y', 'Posted', '1') OR ve.IsApproved IS NULL)
      AND (ve.IsActive IS NULL OR ve.IsActive = '1')
      AND ve.VoucherDate >= @FromDateD AND ve.VoucherDate < @ToDateExcl
      AND (@Main_CompanyCode = '' OR ve.Main_CompanyCode = @Main_CompanyCode OR ve.CompanyCode = @Main_CompanyCode)
      AND (@BranchCode = '' OR ve.BranchCode = @BranchCode)
      AND (gl.GroupLedgerCodeNo IN ('3', '4') OR gl.GroupLedgerCodeNo LIKE '3%' OR gl.GroupLedgerCodeNo LIKE '4%')
    GROUP BY FORMAT(ve.VoucherDate, 'yyyy-MM'), FORMAT(ve.VoucherDate, 'MMM yy')
    ORDER BY MonthKey;

    DROP TABLE #Rows;
    DROP TABLE #Leaf;
    DROP TABLE #Vouchers;
END;
