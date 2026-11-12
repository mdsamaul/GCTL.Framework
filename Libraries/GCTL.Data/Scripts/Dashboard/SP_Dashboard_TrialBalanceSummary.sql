CREATE OR ALTER PROCEDURE dbo.SP_Dashboard_TrialBalanceSummary
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

    -- Execute existing battle-tested Trial Balance SP into temp table
    CREATE TABLE #TB
    (
        [Level] INT,
        Code NVARCHAR(50),
        ParticularName NVARCHAR(250),
        Particulars NVARCHAR(320),
        ParentCode NVARCHAR(50),
        OpeningDebit NUMERIC(18,2),
        OpeningCredit NUMERIC(18,2),
        PeriodDebit NUMERIC(18,2),
        PeriodCredit NUMERIC(18,2),
        ClosingDebit NUMERIC(18,2),
        ClosingCredit NUMERIC(18,2),
        BranchCode NVARCHAR(50)
    );

    INSERT INTO #TB
    EXEC dbo.Rpt_proc_TrialBalanceReportF2 
        @Main_CompanyCode = @Main_CompanyCode,
        @BranchCode = @BranchCode,
        @FromDate = @FromDate,
        @ToDate = @ToDate;

    ---------------------------------------------------------------------
    -- RESULT SET 1: KPIs
    ---------------------------------------------------------------------
    DECLARE @TotClDr NUMERIC(18,2) = 0, @TotClCr NUMERIC(18,2) = 0;
    DECLARE @TotPerDr NUMERIC(18,2) = 0, @TotPerCr NUMERIC(18,2) = 0;

    -- Grand Total is Level 0
    SELECT TOP 1
        @TotClDr = ClosingDebit,
        @TotClCr = ClosingCredit,
        @TotPerDr = PeriodDebit,
        @TotPerCr = PeriodCredit
    FROM #TB
    WHERE [Level] = 0;

    IF @TotClDr = 0 AND @TotClCr = 0
    BEGIN
        SELECT 
            @TotClDr = ISNULL(SUM(ClosingDebit), 0),
            @TotClCr = ISNULL(SUM(ClosingCredit), 0),
            @TotPerDr = ISNULL(SUM(PeriodDebit), 0),
            @TotPerCr = ISNULL(SUM(PeriodCredit), 0)
        FROM #TB
        WHERE [Level] = 1;
    END

    SELECT 
        ISNULL(@TotClDr, 0) AS ClosingDebit,
        ISNULL(@TotClCr, 0) AS ClosingCredit,
        ISNULL(@TotPerDr, 0) AS PeriodDebit,
        ISNULL(@TotPerCr, 0) AS PeriodCredit,
        CASE WHEN ABS(ISNULL(@TotClDr, 0) - ISNULL(@TotClCr, 0)) < 1.0 THEN 1 ELSE 0 END AS IsBalanced,
        ISNULL(@TotClDr, 0) - ISNULL(@TotClCr, 0) AS Difference;

    ---------------------------------------------------------------------
    -- RESULT SET 2: Closing Balance by Group (Level 1)
    ---------------------------------------------------------------------
    SELECT 
        Code,
        ParticularName AS GroupName,
        ClosingDebit,
        ClosingCredit
    FROM #TB
    WHERE [Level] = 1
    ORDER BY Code;

    ---------------------------------------------------------------------
    -- RESULT SET 3: Top Control Ledgers by Debit (Level 2)
    ---------------------------------------------------------------------
    SELECT TOP 8
        Code,
        ParticularName AS ControlName,
        ClosingDebit
    FROM #TB
    WHERE [Level] = 2 AND ClosingDebit > 0
    ORDER BY ClosingDebit DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 4: Full Grid Rows (Level 1 to 5)
    ---------------------------------------------------------------------
    SELECT 
        [Level],
        Code,
        ParticularName,
        Particulars,
        ParentCode,
        OpeningDebit AS OpDr,
        OpeningCredit AS OpCr,
        PeriodDebit AS TrDr,
        PeriodCredit AS TrCr,
        ClosingDebit AS ClDr,
        ClosingCredit AS ClCr
    FROM #TB
    WHERE [Level] BETWEEN 1 AND 5
    ORDER BY Code;

    DROP TABLE #TB;
END;