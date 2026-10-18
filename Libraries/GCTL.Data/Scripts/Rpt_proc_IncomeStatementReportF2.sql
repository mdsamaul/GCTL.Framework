CREATE   PROC [dbo].[Rpt_proc_IncomeStatementReportF2]
    @Main_CompanyCode NVARCHAR(50) = NULL,
    @BranchCode       NVARCHAR(50) = NULL,
    @FromDate         DATETIME     = NULL,
    @ToDate           DATETIME     = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Defaults
    IF @FromDate IS NULL
        SET @FromDate = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);

    IF @ToDate IS NULL
        SET @ToDate = EOMONTH(GETDATE());

    DECLARE @FromDateD  DATE = CAST(@FromDate AS DATE);
    DECLARE @ToDateExcl DATE = DATEADD(DAY, 1, CAST(@ToDate AS DATE));

    SET @Main_CompanyCode = LTRIM(RTRIM(ISNULL(@Main_CompanyCode, '')));
    SET @BranchCode       = LTRIM(RTRIM(ISNULL(@BranchCode, '')));
    IF @BranchCode = 'All' SET @BranchCode = '';

    ------------------------------------------------------------------
    -- 1. Period Vouchers for Group 3 (Income) and Group 4 (Expense)
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
    -- 2. Leaf (General Ledger) level with parents
    ------------------------------------------------------------------
    CREATE TABLE #Leaf
    (
        Section     NVARCHAR(20)  NOT NULL, -- 'Income' or 'Expense'
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
        CASE WHEN g.GroupLedgerCodeNo = '3' OR g.GroupLedgerCodeNo LIKE '3%' THEN 'Income' ELSE 'Expense' END,
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
            WHEN g.GroupLedgerCodeNo = '3' OR g.GroupLedgerCodeNo LIKE '3%' 
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
      )
      AND (
          (CASE WHEN g.GroupLedgerCodeNo = '3' OR g.GroupLedgerCodeNo LIKE '3%' 
                THEN ISNULL(v.PerCr, 0) - ISNULL(v.PerDr, 0)
                ELSE ISNULL(v.PerDr, 0) - ISNULL(v.PerCr, 0)
           END) <> 0
      );

    ------------------------------------------------------------------
    -- 3. Prepare Ordered Output Table
    ------------------------------------------------------------------
    CREATE TABLE #Out
    (
        ID             INT IDENTITY(1,1) PRIMARY KEY,
        [Level]        INT           NOT NULL,
        Code           NVARCHAR(50)  NOT NULL,
        ParticularName NVARCHAR(250) NOT NULL,
        Particulars    NVARCHAR(320) NOT NULL,
        ParentCode     NVARCHAR(50)  NULL,
        Section        NVARCHAR(20)  NOT NULL,
        Debit          NUMERIC(18,2) NOT NULL,
        Credit         NUMERIC(18,2) NOT NULL,
        Amount         NUMERIC(18,2) NOT NULL,
        IsTotal        BIT           NOT NULL,
        SortSection    INT           NOT NULL,
        SortC          NVARCHAR(50)  NOT NULL,
        SortS          NVARCHAR(50)  NOT NULL,
        SortGL         NVARCHAR(50)  NOT NULL,
        SortLevel      INT           NOT NULL
    );

    ------------------------------------------------------------------
    -- 3A. Income Category Header (Level 1)
    ------------------------------------------------------------------
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, SortSection, SortC, SortS, SortGL, SortLevel)
    VALUES (1, N'', N'Income', N'Income', NULL, N'Income', 0, 0, 0, 0, 1, N'', N'', N'', 1);

    ------------------------------------------------------------------
    -- 3B. Income Control Ledgers (Level 2)
    ------------------------------------------------------------------
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, SortSection, SortC, SortS, SortGL, SortLevel)
    SELECT
        2,
        ControlCode,
        ControlName,
        ControlCode + N' â€“ ' + ControlName,
        NULL,
        N'Income',
        SUM(PerDr),
        SUM(PerCr),
        SUM(Amount),
        0,
        1,
        ControlCode,
        N'',
        N'',
        2
    FROM #Leaf
    WHERE Section = 'Income'
    GROUP BY ControlCode, ControlName;

    ------------------------------------------------------------------
    -- 3C. Income SubControl Ledgers (Level 3)
    ------------------------------------------------------------------
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, SortSection, SortC, SortS, SortGL, SortLevel)
    SELECT
        3,
        SubCode,
        SubName,
        SubCode + N' â€“ ' + SubName,
        ControlCode,
        N'Income',
        SUM(PerDr),
        SUM(PerCr),
        SUM(Amount),
        0,
        1,
        ControlCode,
        SubCode,
        N'',
        3
    FROM #Leaf
    WHERE Section = 'Income'
    GROUP BY ControlCode, SubCode, SubName;

    ------------------------------------------------------------------
    -- 3D. Income General Ledgers (Level 5)
    ------------------------------------------------------------------
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, SortSection, SortC, SortS, SortGL, SortLevel)
    SELECT
        5,
        GLCode,
        GLName,
        GLCode + N' â€“ ' + GLName,
        SubCode,
        N'Income',
        PerDr,
        PerCr,
        Amount,
        0,
        1,
        ControlCode,
        SubCode,
        GLCode,
        5
    FROM #Leaf
    WHERE Section = 'Income';

    ------------------------------------------------------------------
    -- 3E. Total Income (Level 1, IsTotal = 1)
    ------------------------------------------------------------------
    DECLARE @TotalIncome NUMERIC(18,2) = ISNULL((SELECT SUM(Amount) FROM #Leaf WHERE Section = 'Income'), 0);
    DECLARE @TotalIncomeDr NUMERIC(18,2) = ISNULL((SELECT SUM(PerDr) FROM #Leaf WHERE Section = 'Income'), 0);
    DECLARE @TotalIncomeCr NUMERIC(18,2) = ISNULL((SELECT SUM(PerCr) FROM #Leaf WHERE Section = 'Income'), 0);

    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, SortSection, SortC, SortS, SortGL, SortLevel)
    VALUES (1, N'', N'Total Income', N'Total Income', NULL, N'Income', @TotalIncomeDr, @TotalIncomeCr, @TotalIncome, 1, 2, N'ZZZ', N'ZZZ', N'ZZZ', 99);

    ------------------------------------------------------------------
    -- 3F. Expenses Category Header (Level 1)
    ------------------------------------------------------------------
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, SortSection, SortC, SortS, SortGL, SortLevel)
    VALUES (1, N'', N'Expenses', N'Expenses', NULL, N'Expense', 0, 0, 0, 0, 3, N'', N'', N'', 1);

    ------------------------------------------------------------------
    -- 3G. Expense Control Ledgers (Level 2)
    ------------------------------------------------------------------
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, SortSection, SortC, SortS, SortGL, SortLevel)
    SELECT
        2,
        ControlCode,
        ControlName,
        ControlCode + N' â€“ ' + ControlName,
        NULL,
        N'Expense',
        SUM(PerDr),
        SUM(PerCr),
        SUM(Amount),
        0,
        3,
        ControlCode,
        N'',
        N'',
        2
    FROM #Leaf
    WHERE Section = 'Expense'
    GROUP BY ControlCode, ControlName;

    ------------------------------------------------------------------
    -- 3H. Expense SubControl Ledgers (Level 3)
    ------------------------------------------------------------------
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, SortSection, SortC, SortS, SortGL, SortLevel)
    SELECT
        3,
        SubCode,
        SubName,
        SubCode + N' â€“ ' + SubName,
        ControlCode,
        N'Expense',
        SUM(PerDr),
        SUM(PerCr),
        SUM(Amount),
        0,
        3,
        ControlCode,
        SubCode,
        N'',
        3
    FROM #Leaf
    WHERE Section = 'Expense'
    GROUP BY ControlCode, SubCode, SubName;

    ------------------------------------------------------------------
    -- 3I. Expense General Ledgers (Level 5)
    ------------------------------------------------------------------
    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, SortSection, SortC, SortS, SortGL, SortLevel)
    SELECT
        5,
        GLCode,
        GLName,
        GLCode + N' â€“ ' + GLName,
        SubCode,
        N'Expense',
        PerDr,
        PerCr,
        Amount,
        0,
        3,
        ControlCode,
        SubCode,
        GLCode,
        5
    FROM #Leaf
    WHERE Section = 'Expense';

    ------------------------------------------------------------------
    -- 3J. Total Expenses (Level 1, IsTotal = 1)
    ------------------------------------------------------------------
    DECLARE @TotalExpense NUMERIC(18,2) = ISNULL((SELECT SUM(Amount) FROM #Leaf WHERE Section = 'Expense'), 0);
    DECLARE @TotalExpenseDr NUMERIC(18,2) = ISNULL((SELECT SUM(PerDr) FROM #Leaf WHERE Section = 'Expense'), 0);
    DECLARE @TotalExpenseCr NUMERIC(18,2) = ISNULL((SELECT SUM(PerCr) FROM #Leaf WHERE Section = 'Expense'), 0);

    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, SortSection, SortC, SortS, SortGL, SortLevel)
    VALUES (1, N'', N'Total Expenses', N'Total Expenses', NULL, N'Expense', @TotalExpenseDr, @TotalExpenseCr, @TotalExpense, 1, 4, N'ZZZ', N'ZZZ', N'ZZZ', 99);

    ------------------------------------------------------------------
    -- 3K. Net Surplus / Deficit (Level 1, IsTotal = 1)
    ------------------------------------------------------------------
    DECLARE @NetSurplus NUMERIC(18,2) = @TotalIncome - @TotalExpense;
    DECLARE @NetName NVARCHAR(100) = CASE WHEN @NetSurplus >= 0 THEN N'Net Surplus for the period' ELSE N'Net Deficit for the period' END;

    INSERT INTO #Out ([Level], Code, ParticularName, Particulars, ParentCode, Section, Debit, Credit, Amount, IsTotal, SortSection, SortC, SortS, SortGL, SortLevel)
    VALUES (1, N'', @NetName, @NetName, NULL, N'Net', 0, 0, @NetSurplus, 1, 5, N'ZZZ', N'ZZZ', N'ZZZ', 100);

    ------------------------------------------------------------------
    -- 4. Final Result Set
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
        @BranchCode AS BranchCode
    FROM #Out
    ORDER BY SortSection, SortC, SortS, SortGL, SortLevel;

    DROP TABLE #Out;
    DROP TABLE #Leaf;
    DROP TABLE #Vouchers;
END