
CREATE PROC [dbo].[Rpt_proc_TrialBalanceReportF2]
    @Main_CompanyCode NVARCHAR(50),
    @BranchCode       NVARCHAR(50),
    @FromDate         DATETIME,
    @ToDate           DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDateD  DATE = CAST(@FromDate AS DATE);
    DECLARE @ToDateExcl DATE = DATEADD(DAY, 1, CAST(@ToDate AS DATE));

    SET @Main_CompanyCode = LTRIM(RTRIM(ISNULL(@Main_CompanyCode, '')));
    SET @BranchCode       = LTRIM(RTRIM(ISNULL(@BranchCode, '')));

    -- Opening table e int, tai int e convert
    DECLARE @OrgInt    INT = TRY_CAST(NULLIF(@Main_CompanyCode, '') AS INT);
    DECLARE @BranchInt INT = TRY_CAST(NULLIF(@BranchCode, '') AS INT);

    ------------------------------------------------------------------
    -- 1. Opening (signed: Dr = +, Cr = -)
    ------------------------------------------------------------------
    CREATE TABLE #Opening
    (
        GeneralLedgerCodeNo NVARCHAR(50) NOT NULL PRIMARY KEY,
        OpeningNet          NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #Opening (GeneralLedgerCodeNo, OpeningNet)
    SELECT
        ob.GeneralLedgerCodeNo,
        SUM(CASE WHEN ob.TrType = 'Dr' THEN  ISNULL(ob.OpeningBalance, 0)
                 WHEN ob.TrType = 'Cr' THEN -ISNULL(ob.OpeningBalance, 0)
                 ELSE 0 END)
    FROM dbo.Acc_Company_Opening_Balance ob
    WHERE ob.DeletedAt IS NULL
      AND (@Main_CompanyCode = '' OR ob.Main_CompanyCode = @OrgInt)
      AND (@BranchCode = ''       OR ob.BranchCode       = @BranchInt)
    GROUP BY ob.GeneralLedgerCodeNo
    OPTION (RECOMPILE);

    ------------------------------------------------------------------
    -- 2. Vouchers (exact string match, Y + Posted approved)
    ------------------------------------------------------------------
    CREATE TABLE #Vouchers
    (
        GeneralLedgerCodeNo NVARCHAR(50) NOT NULL PRIMARY KEY,
        PreDr NUMERIC(18,2) NOT NULL,
        PreCr NUMERIC(18,2) NOT NULL,
        PerDr NUMERIC(18,2) NOT NULL,
        PerCr NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #Vouchers (GeneralLedgerCodeNo, PreDr, PreCr, PerDr, PerCr)
    SELECT
        gl.GeneralLedgerCodeNo,
        SUM(CASE WHEN ve.VoucherDate <  @FromDateD AND ved.TrType = 'Dr' THEN ISNULL(ved.DebitAmount, 0)  ELSE 0 END),
        SUM(CASE WHEN ve.VoucherDate <  @FromDateD AND ved.TrType = 'Cr' THEN ISNULL(ved.CreditAmount, 0) ELSE 0 END),
        SUM(CASE WHEN ve.VoucherDate >= @FromDateD AND ved.TrType = 'Dr' THEN ISNULL(ved.DebitAmount, 0)  ELSE 0 END),
        SUM(CASE WHEN ve.VoucherDate >= @FromDateD AND ved.TrType = 'Cr' THEN ISNULL(ved.CreditAmount, 0) ELSE 0 END)
    FROM dbo.Acc_VoucherEntry ve
    INNER JOIN dbo.Acc_VoucherEntryDetails ved
        ON ved.VoucherEntryCodeNo = ve.VoucherEntryCodeNo
    INNER JOIN dbo.Acc_GeneralLedger gl
        ON gl.GeneralLedgerCodeNo = ved.AccCode
    WHERE ve.IsApproved IN ('Y', 'Posted')
      AND ve.VoucherDate < @ToDateExcl
      AND (@Main_CompanyCode = '' OR ve.Main_CompanyCode = @Main_CompanyCode)
      AND (@BranchCode = ''       OR ve.BranchCode       = @BranchCode)
    GROUP BY gl.GeneralLedgerCodeNo
    OPTION (RECOMPILE);

    ------------------------------------------------------------------
    -- 3. Leaf (General Ledger) level with full parent chain
    ------------------------------------------------------------------
    CREATE TABLE #Leaf
    (
        GroupCode   NVARCHAR(50)  NOT NULL,
        GroupName   NVARCHAR(250) NULL,
        ControlCode NVARCHAR(50)  NOT NULL,
        ControlName NVARCHAR(250) NULL,
        SubCode     NVARCHAR(50)  NOT NULL,
        SubName     NVARCHAR(250) NULL,
        SubSubCode  NVARCHAR(50)  NOT NULL,
        SubSubName  NVARCHAR(250) NULL,
        GLCode      NVARCHAR(50)  NOT NULL,
        GLName      NVARCHAR(250) NULL,
        Opening     NUMERIC(18,2) NOT NULL,
        PerDr       NUMERIC(18,2) NOT NULL,
        PerCr       NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #Leaf
        (GroupCode, GroupName, ControlCode, ControlName, SubCode, SubName,
         SubSubCode, SubSubName, GLCode, GLName, Opening, PerDr, PerCr)
    SELECT
        ISNULL(g.GroupLedgerCodeNo, ''),
        ISNULL(grp.GroupLedgerName, N'(Unassigned)'),
        ISNULL(g.ControlLedgerCodeNo, ''),
        ISNULL(ctl.ControlLedgerName, N'(Unassigned)'),
        ISNULL(g.SubControlLedgerCodeNo, ''),
        ISNULL(sc.SubControlLedgerName, N'(Unassigned)'),
        ISNULL(g.SubSubsidiaryLedgerCodeNo, ''),
        ISNULL(ssl.SubSubsidiaryLedgerName, N'(Unassigned)'),
        g.GeneralLedgerCodeNo,
        g.GeneralLedgerName,
        ISNULL(o.OpeningNet, 0) + ISNULL(v.PreDr, 0) - ISNULL(v.PreCr, 0),
        ISNULL(v.PerDr, 0),
        ISNULL(v.PerCr, 0)
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
    LEFT JOIN dbo.Acc_SubSubsidiaryLedger ssl
        ON ssl.GroupLedgerCodeNo         = g.GroupLedgerCodeNo
       AND ssl.ControlLedgerCodeNo       = g.ControlLedgerCodeNo
       AND ssl.SubControlLedgerCodeNo    = g.SubControlLedgerCodeNo
       AND ssl.SubSubsidiaryLedgerCodeNo = g.SubSubsidiaryLedgerCodeNo
    LEFT JOIN #Opening  o ON o.GeneralLedgerCodeNo = g.GeneralLedgerCodeNo
    LEFT JOIN #Vouchers v ON v.GeneralLedgerCodeNo = g.GeneralLedgerCodeNo
    WHERE ISNULL(o.OpeningNet, 0) <> 0
       OR ISNULL(v.PreDr, 0) <> 0 OR ISNULL(v.PreCr, 0) <> 0
       OR ISNULL(v.PerDr, 0) <> 0 OR ISNULL(v.PerCr, 0) <> 0;

    ------------------------------------------------------------------
    -- 4. Roll-up
    ------------------------------------------------------------------
    CREATE TABLE #R
    (
        LevelNo    INT           NOT NULL,
        Code       NVARCHAR(50)  NOT NULL,
        Name       NVARCHAR(250) NULL,
        ParentCode NVARCHAR(50)  NULL,
        SortG      NVARCHAR(50)  NOT NULL,
        SortC      NVARCHAR(50)  NOT NULL,
        SortS      NVARCHAR(50)  NOT NULL,
        SortSS     NVARCHAR(50)  NOT NULL,
        SortGL     NVARCHAR(50)  NOT NULL,
        Opening    NUMERIC(18,2) NOT NULL,
        PerDr      NUMERIC(18,2) NOT NULL,
        PerCr      NUMERIC(18,2) NOT NULL
    );

    INSERT INTO #R (LevelNo, Code, Name, ParentCode, SortG, SortC, SortS, SortSS, SortGL, Opening, PerDr, PerCr)
    SELECT 1, GroupCode, GroupName, NULL, GroupCode, '', '', '', '',
           SUM(Opening), SUM(PerDr), SUM(PerCr)
    FROM #Leaf
    GROUP BY GroupCode, GroupName;

    INSERT INTO #R (LevelNo, Code, Name, ParentCode, SortG, SortC, SortS, SortSS, SortGL, Opening, PerDr, PerCr)
    SELECT 2, ControlCode, ControlName, GroupCode, GroupCode, ControlCode, '', '', '',
           SUM(Opening), SUM(PerDr), SUM(PerCr)
    FROM #Leaf
    GROUP BY GroupCode, ControlCode, ControlName;

    INSERT INTO #R (LevelNo, Code, Name, ParentCode, SortG, SortC, SortS, SortSS, SortGL, Opening, PerDr, PerCr)
    SELECT 3, SubCode, SubName, ControlCode, GroupCode, ControlCode, SubCode, '', '',
           SUM(Opening), SUM(PerDr), SUM(PerCr)
    FROM #Leaf
    GROUP BY GroupCode, ControlCode, SubCode, SubName;

    INSERT INTO #R (LevelNo, Code, Name, ParentCode, SortG, SortC, SortS, SortSS, SortGL, Opening, PerDr, PerCr)
    SELECT 4, SubSubCode, SubSubName, SubCode, GroupCode, ControlCode, SubCode, SubSubCode, '',
           SUM(Opening), SUM(PerDr), SUM(PerCr)
    FROM #Leaf
    GROUP BY GroupCode, ControlCode, SubCode, SubSubCode, SubSubName;

    INSERT INTO #R (LevelNo, Code, Name, ParentCode, SortG, SortC, SortS, SortSS, SortGL, Opening, PerDr, PerCr)
    SELECT 5, GLCode, GLName, SubSubCode, GroupCode, ControlCode, SubCode, SubSubCode, GLCode,
           Opening, PerDr, PerCr
    FROM #Leaf;

    ------------------------------------------------------------------
    -- 5. Final output
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
        SortG  NVARCHAR(50),
        SortC  NVARCHAR(50),
        SortS  NVARCHAR(50),
        SortSS NVARCHAR(50),
        SortGL NVARCHAR(50),
        IsTotal BIT
    );

    INSERT INTO #Out
    SELECT
        LevelNo,
        Code,
        Name,
        Code + N' - ' + ISNULL(Name, N''),
        ParentCode,
        CASE WHEN Opening > 0 THEN  Opening ELSE 0 END,
        CASE WHEN Opening < 0 THEN -Opening ELSE 0 END,
        PerDr,
        PerCr,
        CASE WHEN Opening + PerDr - PerCr > 0 THEN  (Opening + PerDr - PerCr) ELSE 0 END,
        CASE WHEN Opening + PerDr - PerCr < 0 THEN -(Opening + PerDr - PerCr) ELSE 0 END,
        SortG, SortC, SortS, SortSS, SortGL,
        0
    FROM #R;

    -- Grand Total: shudhu GL (Level 5) rows er sum, jeno Dr/Cr thikmoto mile
    INSERT INTO #Out
    SELECT
        0, N'', N'Grand Total', N'Grand Total', NULL,
        SUM(OpeningDebit), SUM(OpeningCredit),
        SUM(PeriodDebit),  SUM(PeriodCredit),
        SUM(ClosingDebit), SUM(ClosingCredit),
        N'', N'', N'', N'', N'',
        1
    FROM #Out
    WHERE [Level] = 5;

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
    DROP TABLE #R;
    DROP TABLE #Leaf;
    DROP TABLE #Vouchers;
    DROP TABLE #Opening;
END
