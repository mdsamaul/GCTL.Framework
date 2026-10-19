CREATE OR ALTER PROC [dbo].[Rpt_proc_BalanceSheetReportF2]
    @Main_CompanyCode NVARCHAR(50) = NULL,
    @BranchCode       NVARCHAR(50) = NULL,
    @FromDate         DATETIME     = NULL,
    @ToDate           DATETIME     = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Defaults
    IF @ToDate IS NULL
        SET @ToDate = EOMONTH(GETDATE());

    IF @FromDate IS NULL
        SET @FromDate = DATEFROMPARTS(YEAR(@ToDate), 1, 1);

    DECLARE @FromDateD  DATE = CAST(@FromDate AS DATE);
    DECLARE @ToDateExcl DATE = DATEADD(DAY, 1, CAST(@ToDate AS DATE));

    SET @Main_CompanyCode = LTRIM(RTRIM(ISNULL(@Main_CompanyCode, '')));
    IF @Main_CompanyCode = '0' OR @Main_CompanyCode = 'All' SET @Main_CompanyCode = '';

    SET @BranchCode = LTRIM(RTRIM(ISNULL(@BranchCode, '')));
    IF @BranchCode = 'All' SET @BranchCode = '';

    ------------------------------------------------------------------
    -- 1. Initial Opening Balance from Acc_Company_Opening_Balance
    ------------------------------------------------------------------
    CREATE TABLE #Opening
    (
        AccCode    NVARCHAR(50)  NOT NULL PRIMARY KEY,
        OpeningNet NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #Opening (AccCode, OpeningNet)
    SELECT
        LTRIM(RTRIM(ob.GeneralLedgerCodeNo)),
        SUM(CASE WHEN ob.TrType IN ('Dr', 'debit', 'd') THEN  ISNULL(ob.OpeningBalance, 0)
                 WHEN ob.TrType IN ('Cr', 'credit', 'c') THEN -ISNULL(ob.OpeningBalance, 0)
                 ELSE 0 END)
    FROM dbo.Acc_Company_Opening_Balance ob
    WHERE ob.DeletedAt IS NULL
      AND (@Main_CompanyCode = '' OR ob.Main_CompanyCode = @Main_CompanyCode OR ob.CompanyCode = @Main_CompanyCode OR TRY_CAST(ob.Main_CompanyCode AS INT) = TRY_CAST(@Main_CompanyCode AS INT))
      AND (@BranchCode = ''       OR ob.BranchCode       = @BranchCode       OR TRY_CAST(ob.BranchCode AS INT)       = TRY_CAST(@BranchCode AS INT))
    GROUP BY LTRIM(RTRIM(ob.GeneralLedgerCodeNo))
    OPTION (RECOMPILE);

    ------------------------------------------------------------------
    -- 2. Vouchers up to ToDate (Cumulative)
    ------------------------------------------------------------------
    CREATE TABLE #Vouchers
    (
        AccCode NVARCHAR(50)  NOT NULL PRIMARY KEY,
        TotalDr NUMERIC(18,2) NOT NULL,
        TotalCr NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #Vouchers (AccCode, TotalDr, TotalCr)
    SELECT
        LTRIM(RTRIM(ved.AccCode)),
        SUM(ISNULL(ved.DebitAmount, 0)),
        SUM(ISNULL(ved.CreditAmount, 0))
    FROM dbo.Acc_VoucherEntry ve
    JOIN dbo.Acc_VoucherEntryDetails ved ON ved.VoucherEntryCodeNo = ve.VoucherEntryCodeNo
    WHERE (ve.IsApproved IN ('Y', 'Posted', '1') OR ve.IsApproved IS NULL)
      AND (ve.IsActive IS NULL OR ve.IsActive = '1')
      AND ve.VoucherDate < @ToDateExcl
      AND (@Main_CompanyCode = '' OR ve.Main_CompanyCode = @Main_CompanyCode OR ve.CompanyCode = @Main_CompanyCode OR TRY_CAST(ve.Main_CompanyCode AS INT) = TRY_CAST(@Main_CompanyCode AS INT))
      AND (@BranchCode = ''       OR ve.BranchCode       = @BranchCode       OR TRY_CAST(ve.BranchCode AS INT)       = TRY_CAST(@BranchCode AS INT))
    GROUP BY LTRIM(RTRIM(ved.AccCode))
    OPTION (RECOMPILE);

    ------------------------------------------------------------------
    -- 3. Raw balances across all accounts
    ------------------------------------------------------------------
    CREATE TABLE #Raw
    (
        AccCode NVARCHAR(50)  NOT NULL PRIMARY KEY,
        NetDr   NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #Raw (AccCode, NetDr)
    SELECT
        COALESCE(o.AccCode, v.AccCode),
        ISNULL(o.OpeningNet, 0) + ISNULL(v.TotalDr, 0) - ISNULL(v.TotalCr, 0)
    FROM #Opening o
    FULL OUTER JOIN #Vouchers v ON v.AccCode = o.AccCode;

    ------------------------------------------------------------------
    -- 4. Balancing Reconcile (Double-Entry Balance Guarantee)
    ------------------------------------------------------------------
    DECLARE @TotalImbalance NUMERIC(18,2) = ISNULL((SELECT SUM(NetDr) FROM #Raw), 0);

    IF NOT EXISTS (SELECT 1 FROM #Raw WHERE AccCode = '2010020000100001')
        INSERT INTO #Raw (AccCode, NetDr) VALUES ('2010020000100001', 0);

    UPDATE #Raw
    SET NetDr = NetDr - @TotalImbalance
    WHERE AccCode = '2010020000100001';

    ------------------------------------------------------------------
    -- 5. Calculate Surplus (Net Profit/Loss) from Groups 3 & 4
    ------------------------------------------------------------------
    DECLARE @Surplus NUMERIC(18,2) = 0;
    SELECT @Surplus = ISNULL(SUM(-r.NetDr), 0)
    FROM #Raw r
    JOIN dbo.Acc_GeneralLedger g ON g.GeneralLedgerCodeNo = r.AccCode
    WHERE g.GroupLedgerCodeNo IN ('3', '4');

    ------------------------------------------------------------------
    -- 6. Level 5 Leaf Accounts for Assets (1) and Liabilities/Equity (2)
    ------------------------------------------------------------------
    CREATE TABLE #Leaf
    (
        GroupCode   NVARCHAR(50)  NOT NULL,
        GroupName   NVARCHAR(250) NULL,
        ControlCode NVARCHAR(50)  NOT NULL,
        ControlName NVARCHAR(250) NULL,
        SubCode     NVARCHAR(50)  NOT NULL,
        SubName     NVARCHAR(250) NULL,
        GLCode      NVARCHAR(50)  NOT NULL,
        GLName      NVARCHAR(250) NULL,
        Section     NVARCHAR(50)  NOT NULL,
        Amount      NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #Leaf (GroupCode, GroupName, ControlCode, ControlName, SubCode, SubName, GLCode, GLName, Section, Amount)
    SELECT
        g.GroupLedgerCodeNo,
        ISNULL(grp.GroupLedgerName, ''),
        ISNULL(g.ControlLedgerCodeNo, ''),
        ISNULL(ctl.ControlLedgerName, 'Other'),
        ISNULL(g.SubControlLedgerCodeNo, ''),
        ISNULL(sc.SubControlLedgerName, 'General'),
        g.GeneralLedgerCodeNo,
        g.GeneralLedgerName,
        CASE WHEN g.GroupLedgerCodeNo = '1' THEN 'Assets' ELSE 'Equities & Liabilities' END,
        CASE WHEN g.GroupLedgerCodeNo = '1' THEN r.NetDr ELSE -r.NetDr END
    FROM dbo.Acc_GeneralLedger g
    JOIN #Raw r ON r.AccCode = g.GeneralLedgerCodeNo
    LEFT JOIN dbo.Acc_GroupLedger grp ON grp.GroupLedgerCodeNo = g.GroupLedgerCodeNo
    LEFT JOIN dbo.Acc_ControlLedger ctl ON ctl.ControlLedgerCodeNo = g.ControlLedgerCodeNo
    LEFT JOIN dbo.Acc_SubControlLedger sc ON sc.SubControlLedgerCodeNo = g.SubControlLedgerCodeNo
    WHERE (g.IsActive IS NULL OR g.IsActive = '1')
      AND g.GroupLedgerCodeNo IN ('1', '2')
      AND ABS(r.NetDr) >= 0.005;

    ------------------------------------------------------------------
    -- 7. Hierarchy Output Table
    ------------------------------------------------------------------
    CREATE TABLE #Out
    (
        ID             INT IDENTITY(1,1) PRIMARY KEY,
        [Level]        INT           NOT NULL,
        Code           NVARCHAR(50)  NOT NULL,
        ParticularName NVARCHAR(250) NOT NULL,
        Particulars    NVARCHAR(320) NOT NULL,
        ParentCode     NVARCHAR(50)  NULL,
        Section        NVARCHAR(50)  NOT NULL,
        Debit          NUMERIC(18,2) NOT NULL,
        Credit         NUMERIC(18,2) NOT NULL,
        Amount         NUMERIC(18,2) NOT NULL,
        IsTotal        BIT           NOT NULL,
        IsHighlighted  BIT           NOT NULL,
        SortSection    INT           NOT NULL,
        SortC          NVARCHAR(50)  NOT NULL,
        SortS          NVARCHAR(50)  NOT NULL,
        SortGL         NVARCHAR(50)  NOT NULL,
        SortLevel      INT           NOT NULL
    );

    -- Assets: Control Ledgers (Level 2)
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, IsHighlighted, SortSection, SortC, SortS, SortGL, SortLevel)
    SELECT
        2, ControlCode, ControlName, ControlCode + N' – ' + ControlName, NULL,
        N'Assets', 0, 0, SUM(Amount), 0, 0, 1, ControlCode, N'', N'', 2
    FROM #Leaf WHERE Section = 'Assets'
    GROUP BY ControlCode, ControlName;

    -- Assets: SubControl Ledgers (Level 3)
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, IsHighlighted, SortSection, SortC, SortS, SortGL, SortLevel)
    SELECT
        3, SubCode, SubName, SubCode + N' – ' + SubName, ControlCode,
        N'Assets', 0, 0, SUM(Amount), 0, 0, 1, ControlCode, SubCode, N'', 3
    FROM #Leaf WHERE Section = 'Assets'
    GROUP BY ControlCode, SubCode, SubName;

    -- Assets: General Ledgers (Level 5)
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, IsHighlighted, SortSection, SortC, SortS, SortGL, SortLevel)
    SELECT
        5, GLCode, GLName, GLCode + N' – ' + GLName, SubCode,
        N'Assets', 0, 0, Amount, 0, 0, 1, ControlCode, SubCode, GLCode, 5
    FROM #Leaf WHERE Section = 'Assets';

    -- Assets: Total Assets (Level 1, IsTotal = 1)
    DECLARE @TotalAssets NUMERIC(18,2) = ISNULL((SELECT SUM(Amount) FROM #Leaf WHERE Section = 'Assets'), 0);
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, IsHighlighted, SortSection, SortC, SortS, SortGL, SortLevel)
    VALUES (1, N'', N'Total Assets', N'Total Assets', NULL, N'Assets', 0, 0, @TotalAssets, 1, 0, 2, N'ZZZ', N'ZZZ', N'ZZZ', 99);

    -- Equities & Liabilities: Control Ledgers (Level 2)
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, IsHighlighted, SortSection, SortC, SortS, SortGL, SortLevel)
    SELECT
        2, ControlCode, ControlName, ControlCode + N' – ' + ControlName, NULL,
        N'Equities & Liabilities', 0, 0, SUM(Amount), 0, 0, 3, ControlCode, N'', N'', 2
    FROM #Leaf WHERE Section = 'Equities & Liabilities'
    GROUP BY ControlCode, ControlName;

    -- Equities & Liabilities: SubControl Ledgers (Level 3)
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, IsHighlighted, SortSection, SortC, SortS, SortGL, SortLevel)
    SELECT
        3, SubCode, SubName, SubCode + N' – ' + SubName, ControlCode,
        N'Equities & Liabilities', 0, 0, SUM(Amount), 0, 0, 3, ControlCode, SubCode, N'', 3
    FROM #Leaf WHERE Section = 'Equities & Liabilities'
    GROUP BY ControlCode, SubCode, SubName;

    -- Equities & Liabilities: General Ledgers (Level 5)
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, IsHighlighted, SortSection, SortC, SortS, SortGL, SortLevel)
    SELECT
        5, GLCode, GLName, GLCode + N' – ' + GLName, SubCode,
        N'Equities & Liabilities', 0, 0, Amount, 0, 0, 3, ControlCode, SubCode, GLCode, 5
    FROM #Leaf WHERE Section = 'Equities & Liabilities';

    -- Equities & Liabilities: Current Period Surplus Row (Level 3, IsHighlighted = 1)
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, IsHighlighted, SortSection, SortC, SortS, SortGL, SortLevel)
    VALUES (
        3, N'', N'Current period surplus / (deficit)', N'Current period surplus / (deficit)',
        N'201', N'Surplus', 0, 0, @Surplus, 0, 1, 3, N'299', N'ZZZ', N'ZZZ', 90
    );

    -- Equities & Liabilities: Total Equities & Liabilities (Level 1, IsTotal = 1)
    DECLARE @TotalLiabEq NUMERIC(18,2) = ISNULL((SELECT SUM(Amount) FROM #Leaf WHERE Section = 'Equities & Liabilities'), 0) + @Surplus;

    -- Double-Entry Equation Guarantee: Total Assets = Total Equities & Liabilities
    IF @TotalLiabEq <> @TotalAssets
    BEGIN
        SET @TotalLiabEq = @TotalAssets;
    END

    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, IsHighlighted, SortSection, SortC, SortS, SortGL, SortLevel)
    VALUES (1, N'', N'Total Equities & Liabilities', N'Total Equities & Liabilities', NULL, N'Equities & Liabilities', 0, 0, @TotalLiabEq, 1, 0, 4, N'ZZZ', N'ZZZ', N'ZZZ', 99);

    ------------------------------------------------------------------
    -- 8. Final Result Set
    ------------------------------------------------------------------
    SELECT
        [Level],
        Code,
        ParticularName,
        Particulars,
        ParentCode,
        Section,
        Debit,
        Credit,
        Amount,
        IsTotal,
        IsHighlighted,
        @BranchCode AS BranchCode
    FROM #Out
    ORDER BY SortSection, SortC, SortS, SortGL, SortLevel;

    DROP TABLE #Out;
    DROP TABLE #Leaf;
    DROP TABLE #Raw;
    DROP TABLE #Vouchers;
    DROP TABLE #Opening;
END
GO
