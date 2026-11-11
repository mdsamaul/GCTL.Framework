IF OBJECT_ID('dbo.SP_Dashboard_AccountsSummary', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_Dashboard_AccountsSummary;
GO

CREATE PROCEDURE dbo.SP_Dashboard_AccountsSummary
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @BranchCode NVARCHAR(50) = ''
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -3, CAST(GETDATE() AS DATE));
    IF @ToDate IS NULL SET @ToDate = CAST(GETDATE() AS DATE);
    SET @BranchCode = LTRIM(RTRIM(ISNULL(@BranchCode, '')));

    ---------------------------------------------------------------------
    -- 1. All Cash & Bank Ledger Accounts
    ---------------------------------------------------------------------
    SELECT 
        gl.GeneralLedgerCodeNo AS AccCode,
        gl.GeneralLedgerName AS AccName,
        CASE WHEN gl.GeneralLedgerCodeNo LIKE '10200400003%' THEN 'Bank' ELSE 'Cash' END AS AccType
    INTO #Accs
    FROM Acc_GeneralLedger gl WITH (NOLOCK)
    WHERE gl.GeneralLedgerCodeNo LIKE '102004%';

    CREATE CLUSTERED INDEX IX_Accs ON #Accs(AccCode);

    ---------------------------------------------------------------------
    -- 2. Opening Balance before @FromDate
    ---------------------------------------------------------------------
    SELECT 
        t.AccCode,
        SUM(t.NetBal) AS OpeningBal
    INTO #Op
    FROM (
        SELECT 
            ob.GeneralLedgerCodeNo AS AccCode,
            SUM(CASE WHEN ob.TrType = 'Dr' THEN ISNULL(ob.OpeningBalance, 0) ELSE -ISNULL(ob.OpeningBalance, 0) END) AS NetBal
        FROM dbo.Acc_Company_Opening_Balance ob WITH (NOLOCK)
        WHERE ob.DeletedAt IS NULL AND ob.GeneralLedgerCodeNo LIKE '102004%'
        GROUP BY ob.GeneralLedgerCodeNo
        
        UNION ALL
        
        SELECT 
            ved.AccCode,
            SUM(CASE WHEN ved.TrType = 'Dr' THEN ved.DebitAmount ELSE -ved.CreditAmount END) AS NetBal
        FROM dbo.Acc_VoucherEntryDetails ved WITH (NOLOCK)
        JOIN dbo.Acc_VoucherEntry ve WITH (NOLOCK) ON ve.VoucherEntryCodeNo = ved.VoucherEntryCodeNo
        WHERE ved.AccCode LIKE '102004%'
          AND ve.IsApproved IN ('Y', 'Posted')
          AND ve.VoucherDate < @FromDate
          AND (@BranchCode = '' OR ve.BranchCode = @BranchCode)
        GROUP BY ved.AccCode
    ) t
    GROUP BY t.AccCode;

    CREATE CLUSTERED INDEX IX_Op ON #Op(AccCode);

    ---------------------------------------------------------------------
    -- 3. Period Transactions between @FromDate and @ToDate
    ---------------------------------------------------------------------
    SELECT 
        ved.AccCode,
        SUM(ved.DebitAmount) AS PeriodDebit,
        SUM(ved.CreditAmount) AS PeriodCredit
    INTO #Period
    FROM dbo.Acc_VoucherEntryDetails ved WITH (NOLOCK)
    JOIN dbo.Acc_VoucherEntry ve WITH (NOLOCK) ON ve.VoucherEntryCodeNo = ved.VoucherEntryCodeNo
    WHERE ved.AccCode LIKE '102004%'
      AND ve.IsApproved IN ('Y', 'Posted')
      AND ve.VoucherDate >= @FromDate AND ve.VoucherDate <= DATEADD(DAY, 1, @ToDate)
      AND (@BranchCode = '' OR ve.BranchCode = @BranchCode)
    GROUP BY ved.AccCode;

    CREATE CLUSTERED INDEX IX_Period ON #Period(AccCode);

    ---------------------------------------------------------------------
    -- 4. Consolidated Balances
    ---------------------------------------------------------------------
    SELECT 
        a.AccType,
        a.AccCode,
        a.AccName,
        'Head Office' AS Branch,
        ISNULL(op.OpeningBal, 0) AS Opening,
        ISNULL(p.PeriodDebit, 0) AS Receipts,
        ISNULL(p.PeriodCredit, 0) AS Payments,
        ISNULL(op.OpeningBal, 0) + ISNULL(p.PeriodDebit, 0) - ISNULL(p.PeriodCredit, 0) AS Closing
    INTO #Balances
    FROM #Accs a
    LEFT JOIN #Op op ON op.AccCode = a.AccCode
    LEFT JOIN #Period p ON p.AccCode = a.AccCode
    WHERE (ISNULL(op.OpeningBal, 0) <> 0 OR ISNULL(p.PeriodDebit, 0) <> 0 OR ISNULL(p.PeriodCredit, 0) <> 0);

    ---------------------------------------------------------------------
    -- RESULT SET 1: KPIs & Minis
    ---------------------------------------------------------------------
    DECLARE @CashBal NUMERIC(18,2), @BankBal NUMERIC(18,2), @TotalReceipts NUMERIC(18,2), @TotalPayments NUMERIC(18,2), @NetFlow NUMERIC(18,2);
    DECLARE @LargestBank NVARCHAR(150), @LargestBankAmt NUMERIC(18,2), @ActiveAccounts INT;

    SELECT 
        @CashBal = ISNULL(SUM(CASE WHEN AccType = 'Cash' THEN Closing ELSE 0 END), 0),
        @BankBal = ISNULL(SUM(CASE WHEN AccType = 'Bank' THEN Closing ELSE 0 END), 0),
        @TotalReceipts = ISNULL(SUM(Receipts), 0),
        @TotalPayments = ISNULL(SUM(Payments), 0),
        @NetFlow = ISNULL(SUM(Receipts), 0) - ISNULL(SUM(Payments), 0),
        @ActiveAccounts = COUNT(*)
    FROM #Balances;

    SELECT TOP 1 
        @LargestBank = AccName,
        @LargestBankAmt = Closing
    FROM #Balances
    WHERE AccType = 'Bank'
    ORDER BY Closing DESC;

    SELECT 
        ISNULL(@CashBal, 0) AS CashInHand,
        ISNULL(@BankBal, 0) AS BankBalance,
        ISNULL(@TotalReceipts, 0) AS TotalReceipts,
        ISNULL(@TotalPayments, 0) AS TotalPayments,
        ISNULL(@NetFlow, 0) AS NetFlow,
        ISNULL(@LargestBank, '-') AS LargestBank,
        ISNULL(@LargestBankAmt, 0) AS LargestBankAmt,
        ISNULL(@ActiveAccounts, 0) AS ActiveAccounts;

    ---------------------------------------------------------------------
    -- RESULT SET 2: Account Balances Composition (Doughnut)
    ---------------------------------------------------------------------
    SELECT 
        AccName,
        AccType,
        CASE WHEN Closing < 0 THEN 0 ELSE Closing END AS ClosingBalance
    FROM #Balances
    ORDER BY Closing DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 3: Monthly Receipts vs Payments Trend
    ---------------------------------------------------------------------
    SELECT 
        FORMAT(ve.VoucherDate, 'yyyy-MM') AS MonthKey,
        FORMAT(ve.VoucherDate, 'MMM yy') AS MonthLabel,
        ISNULL(SUM(ved.DebitAmount), 0) AS Receipts,
        ISNULL(SUM(ved.CreditAmount), 0) AS Payments,
        ISNULL(SUM(ved.DebitAmount), 0) - ISNULL(SUM(ved.CreditAmount), 0) AS NetFlow
    FROM dbo.Acc_VoucherEntryDetails ved WITH (NOLOCK)
    JOIN dbo.Acc_VoucherEntry ve WITH (NOLOCK) ON ve.VoucherEntryCodeNo = ved.VoucherEntryCodeNo
    WHERE ved.AccCode LIKE '102004%'
      AND ve.IsApproved IN ('Y', 'Posted')
      AND ve.VoucherDate >= @FromDate AND ve.VoucherDate <= DATEADD(DAY, 1, @ToDate)
      AND (@BranchCode = '' OR ve.BranchCode = @BranchCode)
    GROUP BY FORMAT(ve.VoucherDate, 'yyyy-MM'), FORMAT(ve.VoucherDate, 'MMM yy')
    ORDER BY MonthKey ASC;

    ---------------------------------------------------------------------
    -- RESULT SET 4: Grid Detail Rows
    ---------------------------------------------------------------------
    SELECT 
        ROW_NUMBER() OVER (ORDER BY AccType, Closing DESC) AS SN,
        AccType,
        Branch,
        AccCode,
        AccName,
        Opening,
        Receipts,
        Payments,
        Closing
    FROM #Balances
    ORDER BY AccType, Closing DESC;

    DROP TABLE #Accs;
    DROP TABLE #Op;
    DROP TABLE #Period;
    DROP TABLE #Balances;
END;
GO
