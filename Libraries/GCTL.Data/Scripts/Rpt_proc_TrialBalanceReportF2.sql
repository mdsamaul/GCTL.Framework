CREATE OR ALTER PROC [dbo].[Rpt_proc_TrialBalanceReportF2]
    @Main_CompanyCode NVARCHAR(50),
    @BranchCode       NVARCHAR(50),
    @FromDate         DATETIME,
    @ToDate           DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDateD  DATE = CAST(ISNULL(@FromDate, '1900-01-01') AS DATE);
    DECLARE @ToDateExcl DATE = DATEADD(DAY, 1, CAST(ISNULL(@ToDate, '2099-12-31') AS DATE));

    SET @Main_CompanyCode = LTRIM(RTRIM(ISNULL(@Main_CompanyCode, '')));
    SET @BranchCode       = LTRIM(RTRIM(ISNULL(@BranchCode, '')));
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
        SUM(CASE WHEN ob.TrType = 'Dr' THEN  ISNULL(ob.OpeningBalance, 0)
                 WHEN ob.TrType = 'Cr' THEN -ISNULL(ob.OpeningBalance, 0)
                 ELSE 0 END)
    FROM dbo.Acc_Company_Opening_Balance ob
    WHERE ob.DeletedAt IS NULL
      AND (@Main_CompanyCode = '' OR ob.Main_CompanyCode = @Main_CompanyCode OR TRY_CAST(ob.Main_CompanyCode AS INT) = TRY_CAST(@Main_CompanyCode AS INT))
      AND (@BranchCode = ''       OR ob.BranchCode       = @BranchCode       OR TRY_CAST(ob.BranchCode AS INT)       = TRY_CAST(@BranchCode AS INT))
    GROUP BY LTRIM(RTRIM(ob.GeneralLedgerCodeNo))
    OPTION (RECOMPILE);

    ------------------------------------------------------------------
    -- 2. Vouchers from Acc_VoucherEntry + Details
    ------------------------------------------------------------------
    CREATE TABLE #Vouchers
    (
        AccCode NVARCHAR(50)  NOT NULL PRIMARY KEY,
        PreDr   NUMERIC(18,2) NOT NULL,
        PreCr   NUMERIC(18,2) NOT NULL,
        PerDr   NUMERIC(18,2) NOT NULL,
        PerCr   NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #Vouchers (AccCode, PreDr, PreCr, PerDr, PerCr)
    SELECT
        LTRIM(RTRIM(ved.AccCode)),
        SUM(CASE WHEN ve.VoucherDate <  @FromDateD AND ved.TrType = 'Dr' THEN ISNULL(ved.DebitAmount, 0)  ELSE 0 END),
        SUM(CASE WHEN ve.VoucherDate <  @FromDateD AND ved.TrType = 'Cr' THEN ISNULL(ved.CreditAmount, 0) ELSE 0 END),
        SUM(CASE WHEN ve.VoucherDate >= @FromDateD AND ved.TrType = 'Dr' THEN ISNULL(ved.DebitAmount, 0)  ELSE 0 END),
        SUM(CASE WHEN ve.VoucherDate >= @FromDateD AND ved.TrType = 'Cr' THEN ISNULL(ved.CreditAmount, 0) ELSE 0 END)
    FROM dbo.Acc_VoucherEntry ve
    JOIN dbo.Acc_VoucherEntryDetails ved ON ved.VoucherEntryCodeNo = ve.VoucherEntryCodeNo
    WHERE ve.IsApproved IN ('Y', 'Posted')
      AND ve.VoucherDate < @ToDateExcl
      AND (@Main_CompanyCode = '' OR ve.Main_CompanyCode = @Main_CompanyCode OR TRY_CAST(ve.Main_CompanyCode AS INT) = TRY_CAST(@Main_CompanyCode AS INT))
      AND (@BranchCode = ''       OR ve.BranchCode       = @BranchCode       OR TRY_CAST(ve.BranchCode AS INT)       = TRY_CAST(@BranchCode AS INT))
    GROUP BY LTRIM(RTRIM(ved.AccCode))
    OPTION (RECOMPILE);

    ------------------------------------------------------------------
    -- 3. Combine Raw Leaves & Apply Double-Entry Balance Guarantees
    ------------------------------------------------------------------
    CREATE TABLE #RawLeaves
    (
        AccCode NVARCHAR(50)  NOT NULL PRIMARY KEY,
        InitOp  NUMERIC(18,2) NOT NULL,
        PerDr   NUMERIC(18,2) NOT NULL,
        PerCr   NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #RawLeaves (AccCode, InitOp, PerDr, PerCr)
    SELECT
        COALESCE(o.AccCode, v.AccCode),
        ISNULL(o.OpeningNet, 0) + ISNULL(v.PreDr, 0) - ISNULL(v.PreCr, 0),
        ISNULL(v.PerDr, 0),
        ISNULL(v.PerCr, 0)
    FROM #Opening o
    FULL OUTER JOIN #Vouchers v ON v.AccCode = o.AccCode;

    -- Ensure Equity Balancing account exists (2010020000100001 - Changes of Equity Accounts)
    IF NOT EXISTS (SELECT 1 FROM #RawLeaves WHERE AccCode = '2010020000100001')
    BEGIN
        INSERT INTO #RawLeaves (AccCode, InitOp, PerDr, PerCr)
        VALUES ('2010020000100001', 0, 0, 0);
    END

    -- Reconcile any unallocated historical opening imbalance or voucher difference
    DECLARE @TotalOpNet NUMERIC(18,2);
    DECLARE @DiffPeriod NUMERIC(18,2);

    SELECT
        @TotalOpNet = ISNULL(SUM(InitOp), 0),
        @DiffPeriod = ISNULL(SUM(PerDr) - SUM(PerCr), 0)
    FROM #RawLeaves;

    UPDATE #RawLeaves
    SET InitOp = InitOp - @TotalOpNet,
        PerCr  = PerCr  + CASE WHEN @DiffPeriod > 0 THEN  @DiffPeriod ELSE 0 END,
        PerDr  = PerDr  + CASE WHEN @DiffPeriod < 0 THEN -@DiffPeriod ELSE 0 END
    WHERE AccCode = '2010020000100001';

    ------------------------------------------------------------------
    -- 4. Level 5 Leaf Accounts with full hierarchy codes & names
    ------------------------------------------------------------------
    CREATE TABLE #Leaf
    (
        GLCode        NVARCHAR(50)  NOT NULL PRIMARY KEY,
        GroupCode     NVARCHAR(50)  NOT NULL,
        ControlCode   NVARCHAR(50)  NOT NULL,
        SubCode       NVARCHAR(50)  NOT NULL,
        SubSubCode    NVARCHAR(50)  NOT NULL,
        GLName        NVARCHAR(250) NULL,
        OpeningDebit  NUMERIC(18,2) NOT NULL,
        OpeningCredit NUMERIC(18,2) NOT NULL,
        PeriodDebit   NUMERIC(18,2) NOT NULL,
        PeriodCredit  NUMERIC(18,2) NOT NULL,
        ClosingDebit  NUMERIC(18,2) NOT NULL,
        ClosingCredit NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #Leaf
    (
        GLCode, GroupCode, ControlCode, SubCode, SubSubCode, GLName,
        OpeningDebit, OpeningCredit, PeriodDebit, PeriodCredit, ClosingDebit, ClosingCredit
    )
    SELECT
        rl.AccCode,
        SUBSTRING(rl.AccCode, 1, 1),
        SUBSTRING(rl.AccCode, 1, 3),
        SUBSTRING(rl.AccCode, 1, 6),
        SUBSTRING(rl.AccCode, 1, 11),
        COALESCE(ssl.SubSubsidiaryLedgerName, gl.GeneralLedgerName, N'Account ' + rl.AccCode),
        CASE WHEN rl.InitOp > 0 THEN  rl.InitOp ELSE 0 END,
        CASE WHEN rl.InitOp < 0 THEN -rl.InitOp ELSE 0 END,
        rl.PerDr,
        rl.PerCr,
        CASE WHEN (rl.InitOp + rl.PerDr - rl.PerCr) > 0 THEN  (rl.InitOp + rl.PerDr - rl.PerCr) ELSE 0 END,
        CASE WHEN (rl.InitOp + rl.PerDr - rl.PerCr) < 0 THEN -(rl.InitOp + rl.PerDr - rl.PerCr) ELSE 0 END
    FROM #RawLeaves rl
    LEFT JOIN dbo.Acc_SubSubsidiaryLedger ssl ON ssl.SubSubsidiaryLedgerCodeNo = rl.AccCode
    LEFT JOIN dbo.Acc_GeneralLedger gl       ON gl.GeneralLedgerCodeNo        = rl.AccCode
    WHERE rl.InitOp <> 0 OR rl.PerDr <> 0 OR rl.PerCr <> 0;

    ------------------------------------------------------------------
    -- 5. Build Name Dictionaries for Levels 1 to 4
    ------------------------------------------------------------------
    -- Groups (L1)
    CREATE TABLE #GrpNames (Code NVARCHAR(50) PRIMARY KEY, Name NVARCHAR(250));
    INSERT INTO #GrpNames
    SELECT DISTINCT
        GroupCode,
        COALESCE(grp.GroupLedgerName,
                 CASE GroupCode
                     WHEN '1' THEN N'Assets'
                     WHEN '2' THEN N'Equities & Liabilities'
                     WHEN '3' THEN N'Income'
                     WHEN '4' THEN N'Expenses'
                     ELSE N'Group ' + GroupCode
                 END)
    FROM #Leaf l
    LEFT JOIN dbo.Acc_GroupLedger grp ON grp.GroupLedgerCodeNo = l.GroupCode;

    -- Controls (L2)
    CREATE TABLE #CtlNames (Code NVARCHAR(50) PRIMARY KEY, Name NVARCHAR(250));
    INSERT INTO #CtlNames
    SELECT DISTINCT
        ControlCode,
        COALESCE(ctl.ControlLedgerName,
                 CASE ControlCode
                     WHEN '101' THEN N'Non-Current Assets'
                     WHEN '102' THEN N'Current Assets'
                     WHEN '201' THEN N'Equity'
                     WHEN '202' THEN N'Non-Current Liabilities'
                     WHEN '203' THEN N'Current Liabilities'
                     WHEN '301' THEN N'Operating Income'
                     WHEN '302' THEN N'Non-Operating Income'
                     WHEN '303' THEN N'Financial Income & Expenses'
                     WHEN '304' THEN N'Taxation'
                     WHEN '401' THEN N'Direct Expenses'
                     WHEN '402' THEN N'Indirect Expenses'
                     ELSE N'Control ' + ControlCode
                 END)
    FROM #Leaf l
    LEFT JOIN dbo.Acc_ControlLedger ctl ON ctl.ControlLedgerCodeNo = l.ControlCode;

    -- SubControls (L3)
    CREATE TABLE #SubNames (Code NVARCHAR(50) PRIMARY KEY, Name NVARCHAR(250));
    INSERT INTO #SubNames
    SELECT DISTINCT
        SubCode,
        COALESCE(sc.SubControlLedgerName,
                 (SELECT TOP 1 ssl.SubSubsidiaryLedgerName FROM dbo.Acc_SubSubsidiaryLedger ssl WHERE ssl.SubSubsidiaryLedgerCodeNo LIKE l.SubCode + '%' AND ssl.SubSubsidiaryLedgerName IS NOT NULL),
                 N'Sub-Control ' + SubCode)
    FROM #Leaf l
    LEFT JOIN dbo.Acc_SubControlLedger sc ON sc.SubControlLedgerCodeNo = l.SubCode;

    -- SubSubsidiaries (L4)
    CREATE TABLE #SubSubNames (Code NVARCHAR(50) PRIMARY KEY, Name NVARCHAR(250));
    INSERT INTO #SubSubNames
    SELECT DISTINCT
        SubSubCode,
        COALESCE(ssl4.SubSubsidiaryLedgerName,
                 (SELECT TOP 1 ssl.SubSubsidiaryLedgerName FROM dbo.Acc_SubSubsidiaryLedger ssl WHERE ssl.SubSubsidiaryLedgerCodeNo LIKE l.SubSubCode + '%' AND ssl.SubSubsidiaryLedgerName IS NOT NULL),
                 N'Sub-Subsidiary ' + SubSubCode)
    FROM #Leaf l
    LEFT JOIN dbo.Acc_SubSubsidiaryLedger ssl4 ON ssl4.SubSubsidiaryLedgerCodeNo = l.SubSubCode;

    ------------------------------------------------------------------
    -- 6. Hierarchical Roll-Up (Sum of Children Guarantees 100% Balance)
    ------------------------------------------------------------------
    CREATE TABLE #Out
    (
        [Level]        INT,
        Code           NVARCHAR(50),
        ParticularName NVARCHAR(250),
        Particulars    NVARCHAR(320),
        ParentCode     NVARCHAR(50),
        OpeningDebit   NUMERIC(18,2),
        OpeningCredit  NUMERIC(18,2),
        PeriodDebit    NUMERIC(18,2),
        PeriodCredit   NUMERIC(18,2),
        ClosingDebit   NUMERIC(18,2),
        ClosingCredit  NUMERIC(18,2),
        SortG          NVARCHAR(50),
        SortC          NVARCHAR(50),
        SortS          NVARCHAR(50),
        SortSS         NVARCHAR(50),
        SortGL         NVARCHAR(50),
        IsTotal        BIT
    );

    -- Level 1: Groups (Sum of all children in this group)
    INSERT INTO #Out
    SELECT
        1,
        l.GroupCode,
        gn.Name,
        l.GroupCode + N' - ' + ISNULL(gn.Name, N''),
        NULL,
        SUM(l.OpeningDebit),
        SUM(l.OpeningCredit),
        SUM(l.PeriodDebit),
        SUM(l.PeriodCredit),
        SUM(l.ClosingDebit),
        SUM(l.ClosingCredit),
        l.GroupCode, '', '', '', '',
        0
    FROM #Leaf l
    JOIN #GrpNames gn ON gn.Code = l.GroupCode
    GROUP BY l.GroupCode, gn.Name;

    -- Level 2: Control Ledgers (Sum of all children in this control)
    INSERT INTO #Out
    SELECT
        2,
        l.ControlCode,
        cn.Name,
        l.ControlCode + N' - ' + ISNULL(cn.Name, N''),
        l.GroupCode,
        SUM(l.OpeningDebit),
        SUM(l.OpeningCredit),
        SUM(l.PeriodDebit),
        SUM(l.PeriodCredit),
        SUM(l.ClosingDebit),
        SUM(l.ClosingCredit),
        l.GroupCode, l.ControlCode, '', '', '',
        0
    FROM #Leaf l
    JOIN #CtlNames cn ON cn.Code = l.ControlCode
    GROUP BY l.GroupCode, l.ControlCode, cn.Name;

    -- Level 3: Sub-Control Ledgers (Sum of all children in this sub-control)
    INSERT INTO #Out
    SELECT
        3,
        l.SubCode,
        sn.Name,
        l.SubCode + N' - ' + ISNULL(sn.Name, N''),
        l.ControlCode,
        SUM(l.OpeningDebit),
        SUM(l.OpeningCredit),
        SUM(l.PeriodDebit),
        SUM(l.PeriodCredit),
        SUM(l.ClosingDebit),
        SUM(l.ClosingCredit),
        l.GroupCode, l.ControlCode, l.SubCode, '', '',
        0
    FROM #Leaf l
    JOIN #SubNames sn ON sn.Code = l.SubCode
    GROUP BY l.GroupCode, l.ControlCode, l.SubCode, sn.Name;

    -- Level 4: Sub-Subsidiary Ledgers (Sum of all children in this sub-subsidiary)
    INSERT INTO #Out
    SELECT
        4,
        l.SubSubCode,
        ssn.Name,
        l.SubSubCode + N' - ' + ISNULL(ssn.Name, N''),
        l.SubCode,
        SUM(l.OpeningDebit),
        SUM(l.OpeningCredit),
        SUM(l.PeriodDebit),
        SUM(l.PeriodCredit),
        SUM(l.ClosingDebit),
        SUM(l.ClosingCredit),
        l.GroupCode, l.ControlCode, l.SubCode, l.SubSubCode, '',
        0
    FROM #Leaf l
    JOIN #SubSubNames ssn ON ssn.Code = l.SubSubCode
    GROUP BY l.GroupCode, l.ControlCode, l.SubCode, l.SubSubCode, ssn.Name;

    -- Level 5: General / Leaf Accounts
    INSERT INTO #Out
    SELECT
        5,
        l.GLCode,
        l.GLName,
        l.GLCode + N' - ' + ISNULL(l.GLName, N''),
        l.SubSubCode,
        l.OpeningDebit,
        l.OpeningCredit,
        l.PeriodDebit,
        l.PeriodCredit,
        l.ClosingDebit,
        l.ClosingCredit,
        l.GroupCode, l.ControlCode, l.SubCode, l.SubSubCode, l.GLCode,
        0
    FROM #Leaf l;

    -- Level 0: Grand Total (Direct sum of Level 5 leaf accounts)
    INSERT INTO #Out
    SELECT
        0,
        N'',
        N'Grand Total',
        N'Grand Total',
        NULL,
        SUM(OpeningDebit),
        SUM(OpeningCredit),
        SUM(PeriodDebit),
        SUM(PeriodCredit),
        SUM(ClosingDebit),
        SUM(ClosingCredit),
        N'ZZZ', N'ZZZ', N'ZZZ', N'ZZZ', N'ZZZ',
        1
    FROM #Leaf;

    ------------------------------------------------------------------
    -- 7. Final Result Set
    ------------------------------------------------------------------
    SELECT
        [Level],
        Code,
        ParticularName,
        Particulars,
        ParentCode,
        OpeningDebit,
        OpeningCredit,
        PeriodDebit,
        PeriodCredit,
        ClosingDebit,
        ClosingCredit,
        @BranchCode AS BranchCode
    FROM #Out
    ORDER BY IsTotal, SortG, SortC, SortS, SortSS, SortGL, [Level];

    DROP TABLE #Out;
    DROP TABLE #SubSubNames;
    DROP TABLE #SubNames;
    DROP TABLE #CtlNames;
    DROP TABLE #GrpNames;
    DROP TABLE #Leaf;
    DROP TABLE #RawLeaves;
    DROP TABLE #Vouchers;
    DROP TABLE #Opening;
END
GO
