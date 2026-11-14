IF OBJECT_ID('dbo.SP_Dashboard_CashBankSummary', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_Dashboard_CashBankSummary;
GO

CREATE PROCEDURE dbo.SP_Dashboard_CashBankSummary
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @BranchCode NVARCHAR(50) = '',
    @ExcludeUnbalanced BIT = 1 -- 1 = Exclude unbalanced journals (Dr != Cr), 0 = Include all
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -3, CAST(GETDATE() AS DATE));
    IF @ToDate IS NULL SET @ToDate = CAST(GETDATE() AS DATE);
    IF @FromDate > @ToDate 
    BEGIN 
        DECLARE @Tmp DATE = @FromDate; 
        SET @FromDate = @ToDate; 
        SET @ToDate = @Tmp; 
    END;

    SET @BranchCode = LTRIM(RTRIM(ISNULL(@BranchCode, '')));
    IF @BranchCode = 'ALL' SET @BranchCode = '';

    ---------------------------------------------------------------------
    -- 0. UNBALANCED VOUCHERS FILTER (Dr <> Cr)
    -- ===================================================================
    -- যে সকল জার্নালে ডেবিট এবং ক্রেডিট অসমান (Debit != Credit), সেগুলোকে এখানে 
    -- ফিল্টার করা হচ্ছে। 
    --
    -- আপনি যদি ভবিষ্যতে সব ভাউচার অন্তর্ভুক্ত করে সব ডাটা পেতে চান, তাহলে:
    -- অপশন ১: নিচে 'INSERT INTO #UnbalancedVouchers...' ব্লকটি কমেন্ট (--) করে দিন।
    -- অপশন ২: অথবা এক্সিকিউট করার সময় @ExcludeUnbalanced = 0 পাস করুন।
    ---------------------------------------------------------------------
    CREATE TABLE #UnbalancedVouchers (
        VoucherEntryCodeNo NVARCHAR(100) PRIMARY KEY
    );

    /* >>> [START: UNBALANCED VOUCHERS INSERT BLOCK] <<<
       ভবিষ্যতে আনব্যালান্সড জার্নাল বাদ না দিয়ে সব ডাটা পেতে চাইলে নিচের ৫টি লাইনের 
       শুরুতে '--' (কমেন্ট) করে দিন:
    */
    IF @ExcludeUnbalanced = 1
    BEGIN
        INSERT INTO #UnbalancedVouchers (VoucherEntryCodeNo)
        SELECT ved.VoucherEntryCodeNo
        FROM dbo.Acc_VoucherEntryDetails ved WITH (NOLOCK)
        GROUP BY ved.VoucherEntryCodeNo
        HAVING ABS(SUM(ved.DebitAmount) - SUM(ved.CreditAmount)) > 0.01;
    END;
    /* >>> [END: UNBALANCED VOUCHERS INSERT BLOCK] <<< */

    ---------------------------------------------------------------------
    -- 1. All Cash & Bank Ledger Accounts from Database
    ---------------------------------------------------------------------
    SELECT 
        gl.GeneralLedgerCodeNo AS AccCode,
        gl.GeneralLedgerName AS RawAccName,
        CASE 
            WHEN gl.GeneralLedgerCodeNo LIKE '10200400003%' THEN 'Bank' 
            ELSE 'Cash' 
        END AS AccType,
        CASE 
            WHEN gl.GeneralLedgerCodeNo LIKE '10200400003%' AND bank.BankName IS NOT NULL
                THEN bank.BankName + ' (' + gl.GeneralLedgerName + ')'
            ELSE gl.GeneralLedgerName
        END AS AccName,
        CASE 
            WHEN gl.GeneralLedgerName LIKE '%Pangoan%' THEN 'Pangaon Division'
            WHEN gl.GeneralLedgerName LIKE '%Benalpole%' THEN 'Benapole'
            WHEN gl.GeneralLedgerName LIKE '%Comilla%' THEN 'Comilla'
            ELSE ISNULL(br.BranchName, 'Head Office')
        END AS Branch
    INTO #Accs
    FROM dbo.Acc_GeneralLedger gl WITH (NOLOCK)
    LEFT JOIN (
        SELECT bai.AccountNO, bi.BankName, bai.AccountName, bai.BranchID
        FROM dbo.Core_BankAccountInformation bai WITH (NOLOCK)
        LEFT JOIN dbo.SALES_Def_BankInfo bi WITH (NOLOCK) ON bi.BankID = bai.BankID
    ) bank ON bank.AccountNO = gl.GeneralLedgerName
    LEFT JOIN dbo.Core_Branch br WITH (NOLOCK) ON RIGHT('00' + LTRIM(RTRIM(br.BranchCode)), 3) = bank.BranchID
    WHERE gl.GeneralLedgerCodeNo LIKE '102004%';

    CREATE CLUSTERED INDEX IX_Accs ON #Accs(AccCode);

    ---------------------------------------------------------------------
    -- 2. Opening Balance before @FromDate
    -- Note: Bank accounts are central company treasury accounts where funds are deposited
    -- at Head Office and disbursed across branches; therefore, Bank accounts evaluate company-wide
    -- to prevent artificial negative branch balances.
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
        WHERE ob.DeletedAt IS NULL 
          AND ob.GeneralLedgerCodeNo LIKE '102004%'
          AND (@BranchCode = '' OR ob.BranchCode = @BranchCode OR ob.GeneralLedgerCodeNo LIKE '10200400003%')
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
          AND (@BranchCode = '' OR ve.BranchCode = @BranchCode OR ved.AccCode LIKE '10200400003%')
          AND NOT EXISTS (SELECT 1 FROM #UnbalancedVouchers uv WHERE uv.VoucherEntryCodeNo = ve.VoucherEntryCodeNo)
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
      AND (@BranchCode = '' OR ve.BranchCode = @BranchCode OR ved.AccCode LIKE '10200400003%')
      AND NOT EXISTS (SELECT 1 FROM #UnbalancedVouchers uv WHERE uv.VoucherEntryCodeNo = ve.VoucherEntryCodeNo)
    GROUP BY ved.AccCode;

    CREATE CLUSTERED INDEX IX_Period ON #Period(AccCode);

    ---------------------------------------------------------------------
    -- 4. Consolidated Cash & Bank Balances (Includes ALL Accounts from DB)
    ---------------------------------------------------------------------
    SELECT 
        a.AccType,
        a.AccCode,
        a.AccName,
        a.Branch,
        ISNULL(op.OpeningBal, 0) AS Opening,
        ISNULL(p.PeriodDebit, 0) AS Receipts,
        ISNULL(p.PeriodCredit, 0) AS Payments,
        ISNULL(op.OpeningBal, 0) + ISNULL(p.PeriodDebit, 0) - ISNULL(p.PeriodCredit, 0) AS Closing
    INTO #Balances
    FROM #Accs a
    LEFT JOIN #Op op ON op.AccCode = a.AccCode
    LEFT JOIN #Period p ON p.AccCode = a.AccCode;

    ---------------------------------------------------------------------
    -- 5. General Ledger Overview Metrics (Trial Balance, Assets, Liab, Working Capital)
    ---------------------------------------------------------------------
    DECLARE @CashBal NUMERIC(18,2) = 0, @BankBal NUMERIC(18,2) = 0;
    DECLARE @TotalReceipts NUMERIC(18,2) = 0, @TotalPayments NUMERIC(18,2) = 0;

    SELECT 
        @CashBal = ISNULL(SUM(CASE WHEN AccType = 'Cash' THEN Closing ELSE 0 END), 0),
        @BankBal = ISNULL(SUM(CASE WHEN AccType = 'Bank' THEN Closing ELSE 0 END), 0),
        @TotalReceipts = ISNULL(SUM(Receipts), 0),
        @TotalPayments = ISNULL(SUM(Payments), 0)
    FROM #Balances;

    -- Trial Balance & Balance Sheet balances as of @ToDate
    DECLARE @ClosingDr NUMERIC(18,2) = 0, @ClosingCr NUMERIC(18,2) = 0;
    DECLARE @TotalAssets NUMERIC(18,2) = 0, @TotalLiabilities NUMERIC(18,2) = 0;
    DECLARE @CurrentAssets NUMERIC(18,2) = 0, @CurrentLiabilities NUMERIC(18,2) = 0;
    DECLARE @TradeReceivable NUMERIC(18,2) = 0, @TradePayable NUMERIC(18,2) = 0;

    WITH AllLedgersClosing AS (
        SELECT 
            gl.GeneralLedgerCodeNo,
            LEFT(gl.GeneralLedgerCodeNo, 1) AS RootGroup,
            LEFT(gl.GeneralLedgerCodeNo, 3) AS ControlGroup,
            LEFT(gl.GeneralLedgerCodeNo, 6) AS SubControlGroup,
            SUM(t.Dr) AS TotalDr,
            SUM(t.Cr) AS TotalCr
        FROM dbo.Acc_GeneralLedger gl WITH (NOLOCK)
        JOIN (
            SELECT 
                ob.GeneralLedgerCodeNo,
                CASE WHEN ob.TrType = 'Dr' THEN ISNULL(ob.OpeningBalance, 0) ELSE 0 END AS Dr,
                CASE WHEN ob.TrType = 'Cr' THEN ISNULL(ob.OpeningBalance, 0) ELSE 0 END AS Cr
            FROM dbo.Acc_Company_Opening_Balance ob WITH (NOLOCK)
            WHERE ob.DeletedAt IS NULL
              AND (@BranchCode = '' OR ob.BranchCode = @BranchCode OR ob.GeneralLedgerCodeNo LIKE '10200400003%')

            UNION ALL

            SELECT 
                ved.AccCode AS GeneralLedgerCodeNo,
                ved.DebitAmount AS Dr,
                ved.CreditAmount AS Cr
            FROM dbo.Acc_VoucherEntryDetails ved WITH (NOLOCK)
            JOIN dbo.Acc_VoucherEntry ve WITH (NOLOCK) ON ve.VoucherEntryCodeNo = ved.VoucherEntryCodeNo
            WHERE ve.IsApproved IN ('Y', 'Posted')
              AND ve.VoucherDate <= DATEADD(DAY, 1, @ToDate)
              AND (@BranchCode = '' OR ve.BranchCode = @BranchCode OR ved.AccCode LIKE '10200400003%')
              AND NOT EXISTS (SELECT 1 FROM #UnbalancedVouchers uv WHERE uv.VoucherEntryCodeNo = ve.VoucherEntryCodeNo)
        ) t ON t.GeneralLedgerCodeNo = gl.GeneralLedgerCodeNo
        GROUP BY gl.GeneralLedgerCodeNo
    )
    SELECT 
        @ClosingDr = ISNULL(SUM(CASE WHEN TotalDr - TotalCr > 0 THEN TotalDr - TotalCr ELSE 0 END), 0),
        @ClosingCr = ISNULL(SUM(CASE WHEN TotalCr - TotalDr > 0 THEN TotalCr - TotalDr ELSE 0 END), 0),
        @TotalAssets = ISNULL(SUM(CASE WHEN RootGroup = '1' AND (ControlGroup = '102' OR ControlGroup = '101') THEN TotalDr - TotalCr ELSE 0 END), 0),
        @TotalLiabilities = ISNULL(SUM(CASE WHEN RootGroup = '2' THEN TotalCr - TotalDr ELSE 0 END), 0),
        @CurrentAssets = ISNULL(SUM(CASE WHEN ControlGroup = '102' THEN TotalDr - TotalCr ELSE 0 END), 0),
        @CurrentLiabilities = ISNULL(SUM(CASE WHEN ControlGroup = '202' THEN TotalCr - TotalDr ELSE 0 END), 0),
        @TradeReceivable = ISNULL(SUM(CASE WHEN SubControlGroup = '102001' THEN TotalDr - TotalCr ELSE 0 END), 0),
        @TradePayable = ISNULL(SUM(CASE WHEN SubControlGroup = '202001' THEN TotalCr - TotalDr ELSE 0 END), 0)
    FROM AllLedgersClosing;

    -- Period Net Profit / Revenue / Expense in the selected filtered period
    DECLARE @NetProfit NUMERIC(18,2) = 0, @TotalRevenue NUMERIC(18,2) = 0, @TotalExpense NUMERIC(18,2) = 0;
    SELECT 
        @NetProfit = ISNULL(SUM(CASE WHEN LEFT(ved.AccCode, 1) = '4' THEN (ved.CreditAmount - ved.DebitAmount)
                                     WHEN LEFT(ved.AccCode, 1) = '3' THEN -(ved.DebitAmount - ved.CreditAmount)
                                     ELSE 0 END), 0),
        @TotalRevenue = ISNULL(SUM(CASE WHEN LEFT(ved.AccCode, 1) = '4' THEN (ved.CreditAmount - ved.DebitAmount) ELSE 0 END), 0),
        @TotalExpense = ISNULL(SUM(CASE WHEN LEFT(ved.AccCode, 1) = '3' THEN (ved.DebitAmount - ved.CreditAmount) ELSE 0 END), 0)
    FROM dbo.Acc_VoucherEntryDetails ved WITH (NOLOCK)
    JOIN dbo.Acc_VoucherEntry ve WITH (NOLOCK) ON ve.VoucherEntryCodeNo = ved.VoucherEntryCodeNo
    WHERE ve.IsApproved IN ('Y', 'Posted')
      AND ve.VoucherDate >= @FromDate AND ve.VoucherDate <= DATEADD(DAY, 1, @ToDate)
      AND (@BranchCode = '' OR ve.BranchCode = @BranchCode)
      AND NOT EXISTS (SELECT 1 FROM #UnbalancedVouchers uv WHERE uv.VoucherEntryCodeNo = ve.VoucherEntryCodeNo);

    -- Derived Ratios
    DECLARE @WorkingCapital NUMERIC(18,2) = @CurrentAssets - @CurrentLiabilities;
    DECLARE @CurrentRatio NUMERIC(18,2) = CASE WHEN @CurrentLiabilities > 0 THEN ROUND(@CurrentAssets / @CurrentLiabilities, 2) ELSE 0 END;
    DECLARE @QuickRatio NUMERIC(18,2) = CASE WHEN @CurrentLiabilities > 0 THEN ROUND((@CashBal + @BankBal + @TradeReceivable) / @CurrentLiabilities, 2) ELSE 0 END;
    DECLARE @NetProfitMargin NUMERIC(18,1) = CASE WHEN @TotalRevenue > 0 THEN ROUND((@NetProfit / @TotalRevenue) * 100.0, 1) ELSE 0.0 END;
    DECLARE @IsTrialBalanced BIT = CASE WHEN ABS(@ClosingDr - @ClosingCr) < 1.0 THEN 1 ELSE 0 END;
    DECLARE @TrialBalanceDiff NUMERIC(18,2) = ABS(@ClosingDr - @ClosingCr);

    ---------------------------------------------------------------------
    -- RESULT SET 1: 6 KPIs & 5 Minis
    ---------------------------------------------------------------------
    SELECT 
        @CashBal AS CashInHand,
        @BankBal AS BankBalance,
        @ClosingDr AS TrialBalanceTotal,
        @IsTrialBalanced AS IsTrialBalanced,
        @TrialBalanceDiff AS TrialBalanceDiff,
        @NetProfit AS NetProfit,
        @TotalRevenue AS TotalRevenue,
        @TotalExpense AS TotalExpense,
        @TotalAssets AS TotalAssets,
        @TotalLiabilities AS TotalLiabilities,
        @CurrentAssets AS CurrentAssets,
        @CurrentLiabilities AS CurrentLiabilities,
        @CurrentRatio AS CurrentRatio,
        @TradeReceivable AS TradeReceivable,
        @TradePayable AS TradePayable,
        @WorkingCapital AS WorkingCapital,
        @QuickRatio AS QuickRatio,
        @NetProfitMargin AS NetProfitMargin,
        @TotalReceipts AS TotalReceipts,
        @TotalPayments AS TotalPayments,
        (@TotalReceipts - @TotalPayments) AS NetCashBankFlow;

    ---------------------------------------------------------------------
    -- RESULT SET 2: Cash & Bank Composition (a_ch1 Doughnut)
    ---------------------------------------------------------------------
    SELECT 'Cash in Hand' AS Label, 'Cash' AS AccType, CASE WHEN @CashBal < 0 THEN 0 ELSE @CashBal END AS Amount
    UNION ALL
    SELECT 'Bank' AS Label, 'Bank' AS AccType, CASE WHEN @BankBal < 0 THEN 0 ELSE @BankBal END AS Amount;

    ---------------------------------------------------------------------
    -- 6. Month Sequence CTE for Chart Trends (dynamically covers all filtered months)
    ---------------------------------------------------------------------
    ;WITH MonthsCTE AS (
        SELECT 
            DATEFROMPARTS(YEAR(@FromDate), MONTH(@FromDate), 1) AS MonthStart,
            EOMONTH(DATEFROMPARTS(YEAR(@FromDate), MONTH(@FromDate), 1)) AS MonthEnd
        UNION ALL
        SELECT 
            DATEADD(MONTH, 1, MonthStart),
            EOMONTH(DATEADD(MONTH, 1, MonthStart))
        FROM MonthsCTE
        WHERE DATEADD(MONTH, 1, MonthStart) <= @ToDate
    ),
    AllCashBankVouchers AS (
        SELECT 
            CASE WHEN ob.GeneralLedgerCodeNo LIKE '10200400003%' THEN 'Bank' ELSE 'Cash' END AS AccType,
            CAST('2000-01-01' AS DATE) AS VDate,
            CASE WHEN ob.TrType = 'Dr' THEN ISNULL(ob.OpeningBalance, 0) ELSE -ISNULL(ob.OpeningBalance, 0) END AS NetBal
        FROM dbo.Acc_Company_Opening_Balance ob WITH (NOLOCK)
        WHERE ob.DeletedAt IS NULL AND ob.GeneralLedgerCodeNo LIKE '102004%'
          AND (@BranchCode = '' OR ob.BranchCode = @BranchCode OR ob.GeneralLedgerCodeNo LIKE '10200400003%')

        UNION ALL

        SELECT 
            CASE WHEN ved.AccCode LIKE '10200400003%' THEN 'Bank' ELSE 'Cash' END AS AccType,
            CAST(ve.VoucherDate AS DATE) AS VDate,
            ved.DebitAmount - ved.CreditAmount AS NetBal
        FROM dbo.Acc_VoucherEntryDetails ved WITH (NOLOCK)
        JOIN dbo.Acc_VoucherEntry ve WITH (NOLOCK) ON ve.VoucherEntryCodeNo = ved.VoucherEntryCodeNo
        WHERE ved.AccCode LIKE '102004%'
          AND ve.IsApproved IN ('Y', 'Posted')
          AND (@BranchCode = '' OR ve.BranchCode = @BranchCode OR ved.AccCode LIKE '10200400003%')
          AND NOT EXISTS (SELECT 1 FROM #UnbalancedVouchers uv WHERE uv.VoucherEntryCodeNo = ve.VoucherEntryCodeNo)
    )
    ---------------------------------------------------------------------
    -- RESULT SET 3: Cash + Bank Trend by Month (a_ch2 Month-End Stacked Bar)
    ---------------------------------------------------------------------
    SELECT 
        FORMAT(m.MonthStart, 'yyyy-MM') AS MonthKey,
        FORMAT(m.MonthStart, 'MMM yy') AS MonthLabel,
        CASE WHEN ISNULL(SUM(CASE WHEN v.AccType = 'Cash' AND v.VDate <= (CASE WHEN m.MonthEnd > @ToDate THEN @ToDate ELSE m.MonthEnd END) THEN v.NetBal ELSE 0 END), 0) < 0 
             THEN 0 
             ELSE ISNULL(SUM(CASE WHEN v.AccType = 'Cash' AND v.VDate <= (CASE WHEN m.MonthEnd > @ToDate THEN @ToDate ELSE m.MonthEnd END) THEN v.NetBal ELSE 0 END), 0) 
        END AS CashAmount,
        CASE WHEN ISNULL(SUM(CASE WHEN v.AccType = 'Bank' AND v.VDate <= (CASE WHEN m.MonthEnd > @ToDate THEN @ToDate ELSE m.MonthEnd END) THEN v.NetBal ELSE 0 END), 0) < 0 
             THEN 0 
             ELSE ISNULL(SUM(CASE WHEN v.AccType = 'Bank' AND v.VDate <= (CASE WHEN m.MonthEnd > @ToDate THEN @ToDate ELSE m.MonthEnd END) THEN v.NetBal ELSE 0 END), 0) 
        END AS BankAmount,
        ISNULL(SUM(CASE WHEN v.VDate <= (CASE WHEN m.MonthEnd > @ToDate THEN @ToDate ELSE m.MonthEnd END) THEN v.NetBal ELSE 0 END), 0) AS TotalAmount
    FROM MonthsCTE m
    CROSS JOIN AllCashBankVouchers v
    GROUP BY m.MonthStart, m.MonthEnd
    ORDER BY m.MonthStart
    OPTION (MAXRECURSION 365);

    ---------------------------------------------------------------------
    -- RESULT SET 4: Income vs Expense Monthly (a_ch3 Bar Chart covering all filtered months)
    ---------------------------------------------------------------------
    ;WITH MonthsCTE AS (
        SELECT 
            DATEFROMPARTS(YEAR(@FromDate), MONTH(@FromDate), 1) AS MonthStart,
            EOMONTH(DATEFROMPARTS(YEAR(@FromDate), MONTH(@FromDate), 1)) AS MonthEnd
        UNION ALL
        SELECT 
            DATEADD(MONTH, 1, MonthStart),
            EOMONTH(DATEADD(MONTH, 1, MonthStart))
        FROM MonthsCTE
        WHERE DATEADD(MONTH, 1, MonthStart) <= @ToDate
    ),
    MonthlyIE AS (
        SELECT 
            FORMAT(ve.VoucherDate, 'yyyy-MM') AS MonthKey,
            ISNULL(SUM(CASE WHEN LEFT(ved.AccCode, 1) = '4' THEN ved.CreditAmount - ved.DebitAmount ELSE 0 END), 0) AS Revenue,
            ISNULL(SUM(CASE WHEN LEFT(ved.AccCode, 1) = '3' THEN ved.DebitAmount - ved.CreditAmount ELSE 0 END), 0) AS Expense
        FROM dbo.Acc_VoucherEntryDetails ved WITH (NOLOCK)
        JOIN dbo.Acc_VoucherEntry ve WITH (NOLOCK) ON ve.VoucherEntryCodeNo = ved.VoucherEntryCodeNo
        WHERE ve.IsApproved IN ('Y', 'Posted')
          AND ve.VoucherDate >= @FromDate AND ve.VoucherDate <= DATEADD(DAY, 1, @ToDate)
          AND (@BranchCode = '' OR ve.BranchCode = @BranchCode)
          AND NOT EXISTS (SELECT 1 FROM #UnbalancedVouchers uv WHERE uv.VoucherEntryCodeNo = ve.VoucherEntryCodeNo)
        GROUP BY FORMAT(ve.VoucherDate, 'yyyy-MM')
    )
    SELECT 
        FORMAT(m.MonthStart, 'yyyy-MM') AS MonthKey,
        FORMAT(m.MonthStart, 'MMM yy') AS MonthLabel,
        ISNULL(ie.Revenue, 0) AS Revenue,
        ISNULL(ie.Expense, 0) AS Expense,
        ISNULL(ie.Revenue, 0) - ISNULL(ie.Expense, 0) AS NetProfit
    FROM MonthsCTE m
    LEFT JOIN MonthlyIE ie ON ie.MonthKey = FORMAT(m.MonthStart, 'yyyy-MM')
    ORDER BY m.MonthStart
    OPTION (MAXRECURSION 365);

    ---------------------------------------------------------------------
    -- RESULT SET 5: Grid Detail Rows (a_g) - Every single account from DB
    ---------------------------------------------------------------------
    SELECT 
        ROW_NUMBER() OVER (ORDER BY AccType, Closing DESC, AccCode) AS SN,
        AccType,
        Branch,
        AccCode,
        AccName,
        Opening,
        Receipts,
        Payments,
        Closing
    FROM #Balances
    ORDER BY AccType, Closing DESC, AccCode;

    DROP TABLE #Accs;
    DROP TABLE #Op;
    DROP TABLE #Period;
    DROP TABLE #Balances;
    DROP TABLE #UnbalancedVouchers;
END;
GO
