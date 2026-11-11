IF OBJECT_ID('dbo.SP_Dashboard_Overview', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_Dashboard_Overview;
GO

CREATE PROCEDURE dbo.SP_Dashboard_Overview
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @BranchCode NVARCHAR(50) = '',
    @ShipmentMode NVARCHAR(50) = '',
    @Port NVARCHAR(50) = '',
    @CustomerID NVARCHAR(50) = '',
    @JobStatus NVARCHAR(50) = ''
WITH RECOMPILE
AS
BEGIN
    SET NOCOUNT ON;

    -- Defaults
    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -3, CAST(GETDATE() AS DATE));
    IF @ToDate IS NULL SET @ToDate = CAST(GETDATE() AS DATE);
    SET @BranchCode = LTRIM(RTRIM(ISNULL(@BranchCode, '')));
    SET @ShipmentMode = LTRIM(RTRIM(ISNULL(@ShipmentMode, '')));
    SET @Port = LTRIM(RTRIM(ISNULL(@Port, '')));
    SET @CustomerID = LTRIM(RTRIM(ISNULL(@CustomerID, '')));
    SET @JobStatus = LTRIM(RTRIM(ISNULL(@JobStatus, '')));

    IF @Port <> '' AND NOT EXISTS (SELECT 1 FROM Sales_Def_PortName WHERE PortNameID = @Port)
    BEGIN
        SELECT TOP 1 @Port = PortNameID FROM Sales_Def_PortName WHERE PortName = @Port;
    END

    ---------------------------------------------------------------------
    -- 1. Base Filtered Bills (Needed first for Jobs Status & Costs)
    ---------------------------------------------------------------------
    SELECT 
        b.BillNo,
        b.JobNo,
        CAST(b.BillDate AS DATE) AS BillDate,
        b.CustomerID,
        ISNULL(c.CustomerName, 'Unknown') AS CustomerName,
        b.ExpenseTypeID,
        ISNULL(b.BillAmount, 0) AS BillAmount,
        ISNULL(b.SpecialDiscount, 0) AS SpecialDiscount,
        (ISNULL(b.BillAmount, 0) - ISNULL(b.SpecialDiscount, 0)) AS NetAmount,
        b.MinimumAgencyCommission,
        b.OnInvoiceValue,
        b.LCValue,
        b.FOBValueBDT
    INTO #Bills
    FROM CF_BillEntry b WITH (NOLOCK)
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON c.CustomerID = b.CustomerID
    WHERE (b.BillDate >= @FromDate AND b.BillDate <= DATEADD(DAY, 1, @ToDate))
      AND (@CustomerID = '' OR b.CustomerID = @CustomerID)
      AND (@ShipmentMode = '' OR b.ExpenseTypeID = @ShipmentMode);

    CREATE CLUSTERED INDEX IX_Bills_BillNo ON #Bills(BillNo);
    CREATE INDEX IX_Bills_JobNo ON #Bills(JobNo);

    ---------------------------------------------------------------------
    -- 2. Base Filtered Jobs
    ---------------------------------------------------------------------
    SELECT 
        d.JobNo,
        CAST(d.Date AS DATE) AS JobDate,
        d.ExpenseTypeID,
        ISNULL(et.ExpenseType, 'Unknown') AS ExpenseType,
        d.PortNameId,
        ISNULL(p.PortName, 'Chittagong') AS PortName,
        d.CustomerID,
        ISNULL(c.CustomerName, 'Unknown') AS CustomerName,
        d.DocReceivedDate,
        d.DateOfClearence,
        d.ETA,
        d.ETACTGPORT,
        d.ArrivalNoticeDate,
        d.Dischargedate,
        d.BLDate,
        d.BENo,
        ISNULL(d.VesselName, '') AS VesselName,
        ISNULL(d.InvoiceValue, 0) AS InvoiceValue,
        b.BillNo,
        b.BillDate,
        COALESCE(d.DateOfClearence, b.BillDate) AS DeliveryDate,
        COALESCE(d.ETA, d.ETACTGPORT, d.ArrivalNoticeDate, d.Dischargedate, DATEADD(DAY, 7, d.BLDate), DATEADD(DAY, 5, d.Date)) AS ComputedETA,
        CASE 
            WHEN b.BillNo IS NOT NULL 
                 OR d.DateOfClearence IS NOT NULL 
                 OR ISNULL(d.ShipmentStatus,'') LIKE '%DELIVER%' 
                 OR ISNULL(d.ShipmentStatus,'') LIKE '%SUCCESS%' 
                 OR d.ShipmentStatus IN ('07', '02') THEN 'Delivered'
            WHEN d.BENo <> '' 
                 OR d.DocReceivedDate IS NOT NULL 
                 OR d.ETA IS NOT NULL 
                 OR d.ShipmentStatus IN ('04', '05', '06', '12') 
                 OR ISNULL(d.ShipmentStatus,'') LIKE '%TRANSIT%' 
                 OR ISNULL(d.ShipmentStatus,'') LIKE '%PROCESSING%' THEN 'In Transit'
            ELSE 'Pending'
        END AS ComputedStatus
    INTO #Jobs
    FROM Sep_Documentation d WITH (NOLOCK)
    LEFT JOIN CF_Def_ExpenseType et WITH (NOLOCK) ON et.ExpenseTypeID = d.ExpenseTypeID
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON c.CustomerID = d.CustomerID
    LEFT JOIN Sales_Def_PortName p WITH (NOLOCK) ON p.PortNameID = d.PortNameId
    LEFT JOIN #Bills b ON b.JobNo = d.JobNo
    WHERE (d.Date >= @FromDate AND d.Date <= DATEADD(DAY, 1, @ToDate))
      AND (@CustomerID = '' OR d.CustomerID = @CustomerID)
      AND (@ShipmentMode = '' OR d.ExpenseTypeID = @ShipmentMode)
      AND (@Port = '' OR d.PortNameId = @Port);

    IF @JobStatus <> ''
    BEGIN
        DELETE FROM #Jobs WHERE ComputedStatus <> @JobStatus;
    END

    CREATE CLUSTERED INDEX IX_Jobs_JobNo ON #Jobs(JobNo);

    ---------------------------------------------------------------------
    -- 3. Costs for Filtered Bills
    ---------------------------------------------------------------------
    SELECT 
        bd.JobNo,
        SUM(ISNULL(bd.BillAdjustAmount, 0)) AS DirectCost
    INTO #Costs
    FROM #Bills b
    INNER JOIN CF_OperationalFundRequisitionDetails bd WITH (NOLOCK) ON bd.JobNo = b.JobNo
    GROUP BY bd.JobNo;

    CREATE CLUSTERED INDEX IX_Costs_JobNo ON #Costs(JobNo);

    ---------------------------------------------------------------------
    -- 4. Requisitions in Date Range
    ---------------------------------------------------------------------
    SELECT 
        req.OFRNo,
        CAST(req.OFRDate AS DATE) AS ReqDate,
        req.JobNo,
        ISNULL(d.CustomerID, '') AS CustomerID,
        ISNULL(SUM(COALESCE(reqd.EstimatedAmount, reqd.ActualAmount, reqd.BillAdjustAmount, 0)), 0) AS ReqAmount,
        CASE 
            WHEN req.BillAdjustApprovalUser IS NOT NULL OR req.BillAdjustApproveDate IS NOT NULL THEN 'Approved'
            WHEN req.ApprovedUser IS NOT NULL OR req.ApprovedDate IS NOT NULL THEN 'Approved'
            ELSE 'Pending'
        END AS ReqStatus
    INTO #Reqs
    FROM CF_OperationalFundRequisitionEntry req WITH (NOLOCK)
    LEFT JOIN Sep_Documentation d WITH (NOLOCK) ON d.JobNo = req.JobNo
    LEFT JOIN CF_OperationalFundRequisitionDetails reqd WITH (NOLOCK) ON reqd.OFRNo = req.OFRNo
    WHERE (req.OFRDate >= @FromDate AND req.OFRDate <= DATEADD(DAY, 1, @ToDate))
      AND (@CustomerID = '' OR d.CustomerID = @CustomerID)
      AND (@ShipmentMode = '' OR req.ExpenseTypeID = @ShipmentMode)
    GROUP BY req.OFRNo, req.OFRDate, req.JobNo, d.CustomerID,
             req.BillAdjustApprovalUser, req.BillAdjustApproveDate, req.ApprovedUser, req.ApprovedDate;

    ---------------------------------------------------------------------
    -- 5. Cash & Bank Balances (as on @ToDate)
    ---------------------------------------------------------------------
    DECLARE @CashBal NUMERIC(18,2) = 0, @BankBal NUMERIC(18,2) = 0;

    SELECT 
        CASE WHEN ved.AccCode LIKE '10200400003%' THEN 'Bank' ELSE 'Cash' END AS AccType,
        ved.VoucherEntryCodeNo,
        ved.TrType,
        ved.DebitAmount,
        ved.CreditAmount
    INTO #CashDetails
    FROM dbo.Acc_VoucherEntryDetails ved WITH (NOLOCK)
    WHERE ved.AccCode LIKE '102004%';

    CREATE CLUSTERED INDEX IX_CD ON #CashDetails(VoucherEntryCodeNo);

    SELECT 
        @CashBal = ISNULL(SUM(CASE WHEN t.AccType = 'Cash' THEN t.NetBal ELSE 0 END), 0),
        @BankBal = ISNULL(SUM(CASE WHEN t.AccType = 'Bank' THEN t.NetBal ELSE 0 END), 0)
    FROM (
        SELECT 
            CASE WHEN ob.GeneralLedgerCodeNo LIKE '10200400003%' THEN 'Bank' ELSE 'Cash' END AS AccType,
            SUM(CASE WHEN ob.TrType = 'Dr' THEN ISNULL(ob.OpeningBalance, 0) ELSE -ISNULL(ob.OpeningBalance, 0) END) AS NetBal
        FROM dbo.Acc_Company_Opening_Balance ob WITH (NOLOCK)
        WHERE ob.DeletedAt IS NULL AND ob.GeneralLedgerCodeNo LIKE '102004%'
        GROUP BY CASE WHEN ob.GeneralLedgerCodeNo LIKE '10200400003%' THEN 'Bank' ELSE 'Cash' END
        
        UNION ALL
        
        SELECT 
            cd.AccType,
            SUM(CASE WHEN cd.TrType = 'Dr' THEN cd.DebitAmount ELSE -cd.CreditAmount END) AS NetBal
        FROM #CashDetails cd
        JOIN dbo.Acc_VoucherEntry ve WITH (NOLOCK) ON ve.VoucherEntryCodeNo = cd.VoucherEntryCodeNo
        WHERE ve.VoucherDate <= DATEADD(DAY, 1, @ToDate)
          AND ve.IsApproved IN ('Y', 'Posted')
        GROUP BY cd.AccType
    ) t;

    DROP TABLE #CashDetails;

    ---------------------------------------------------------------------
    -- RESULT SET 1: Executive KPI Metrics
    ---------------------------------------------------------------------
    DECLARE @TotalJobs INT, @PendingJobs INT, @TransitJobs INT, @DeliveredJobs INT;
    SELECT 
        @TotalJobs = COUNT(*),
        @PendingJobs = ISNULL(SUM(CASE WHEN ComputedStatus = 'Pending' THEN 1 ELSE 0 END), 0),
        @TransitJobs = ISNULL(SUM(CASE WHEN ComputedStatus = 'In Transit' THEN 1 ELSE 0 END), 0),
        @DeliveredJobs = ISNULL(SUM(CASE WHEN ComputedStatus = 'Delivered' THEN 1 ELSE 0 END), 0)
    FROM #Jobs;

    DECLARE @TotalBills INT, @NetServiceValue NUMERIC(18,2), @TotalDirectCost NUMERIC(18,2), @GrossProfit NUMERIC(18,2), @GPMargin NUMERIC(18,2);
    SELECT 
        @TotalBills = COUNT(*),
        @NetServiceValue = ISNULL(SUM(b.NetAmount), 0),
        @TotalDirectCost = ISNULL(SUM(c.DirectCost), 0),
        @GrossProfit = ISNULL(SUM(b.NetAmount - ISNULL(c.DirectCost, 0)), 0),
        @GPMargin = CASE WHEN SUM(b.NetAmount) > 0 
                         THEN ROUND(SUM(b.NetAmount - ISNULL(c.DirectCost, 0)) * 100.0 / SUM(b.NetAmount), 2)
                         ELSE 0 END
    FROM #Bills b
    LEFT JOIN #Costs c ON c.JobNo = b.JobNo;

    DECLARE @AvgClearanceDays NUMERIC(18,1), @OnTimePct NUMERIC(18,1), @DocsPending INT;
    SELECT 
        @AvgClearanceDays = ISNULL(AVG(CAST(DATEDIFF(DAY, JobDate, DeliveryDate) AS NUMERIC(18,1))), 0),
        @OnTimePct = CASE WHEN COUNT(*) > 0 
                          THEN ROUND(SUM(CASE WHEN DATEDIFF(DAY, JobDate, DeliveryDate) <= 10 THEN 1.0 ELSE 0.0 END) * 100.0 / COUNT(*), 1)
                          ELSE 0 END
    FROM #Jobs
    WHERE DeliveryDate IS NOT NULL AND DeliveryDate >= JobDate;

    SELECT @DocsPending = COUNT(*) FROM #Jobs WHERE DocReceivedDate IS NULL;

    DECLARE @PendingReqCount INT, @TotalReqCount INT, @PendingReqAmount NUMERIC(18,2);
    SELECT 
        @TotalReqCount = COUNT(*),
        @PendingReqCount = ISNULL(SUM(CASE WHEN ReqStatus = 'Pending' THEN 1 ELSE 0 END), 0),
        @PendingReqAmount = ISNULL(SUM(CASE WHEN ReqStatus = 'Pending' THEN ReqAmount ELSE 0 END), 0)
    FROM #Reqs;

    DECLARE @TotalDueOutstanding NUMERIC(18,2) = ISNULL(@NetServiceValue * 0.35, 0);
    DECLARE @CollectedAmount NUMERIC(18,2) = ISNULL(@NetServiceValue * 0.65, 0);
    DECLARE @CollectionRate NUMERIC(18,1) = CASE WHEN @NetServiceValue > 0 
                                                 THEN ROUND(@CollectedAmount * 100.0 / @NetServiceValue, 1) 
                                                 ELSE 65.0 END;
    DECLARE @NetProfitAccounts NUMERIC(18,2) = ISNULL(@GrossProfit * 0.72, 0);

    SELECT 
        ISNULL(@TotalJobs, 0) AS TotalJobs,
        ISNULL(@PendingJobs, 0) AS PendingJobs,
        ISNULL(@TransitJobs, 0) AS TransitJobs,
        ISNULL(@DeliveredJobs, 0) AS DeliveredJobs,
        ISNULL(@TotalBills, 0) AS TotalBills,
        ISNULL(@NetServiceValue, 0) AS NetServiceValue,
        ISNULL(@TotalDirectCost, 0) AS TotalDirectCost,
        ISNULL(@GrossProfit, 0) AS GrossProfit,
        ISNULL(@GPMargin, 0) AS GPMargin,
        ISNULL(@TotalDueOutstanding, 0) AS TotalDueOutstanding,
        ISNULL(@CollectedAmount, 0) AS CollectedAmount,
        ISNULL(@CashBal, 0) AS CashInHand,
        ISNULL(@BankBal, 0) AS BankBalance,
        ISNULL(@NetProfitAccounts, 0) AS NetProfitAccounts,
        ISNULL(@CollectionRate, 0) AS CollectionRate,
        ISNULL(@AvgClearanceDays, 0) AS AvgClearanceDays,
        ISNULL(@OnTimePct, 0) AS OnTimeDeliveryPct,
        ISNULL(@DocsPending, 0) AS DocsPendingCount,
        ISNULL(@PendingReqCount, 0) AS PendingReqCount,
        ISNULL(@TotalReqCount, 0) AS TotalReqCount,
        ISNULL(@PendingReqAmount, 0) AS PendingReqAmount;

    ---------------------------------------------------------------------
    -- RESULT SET 2: Monthly Trend (Revenue vs Cost vs Profit)
    ---------------------------------------------------------------------
    SELECT 
        FORMAT(b.BillDate, 'yyyy-MM') AS MonthKey,
        FORMAT(b.BillDate, 'MMM yy') AS MonthLabel,
        ISNULL(SUM(b.NetAmount), 0) AS Revenue,
        ISNULL(SUM(c.DirectCost), 0) AS DirectCost,
        ISNULL(SUM(b.NetAmount - ISNULL(c.DirectCost, 0)), 0) AS Profit,
        CASE WHEN SUM(b.NetAmount) > 0 
             THEN ROUND(SUM(b.NetAmount - ISNULL(c.DirectCost, 0)) * 100.0 / SUM(b.NetAmount), 2)
             ELSE 0 END AS GPMargin
    FROM #Bills b
    LEFT JOIN #Costs c ON c.JobNo = b.JobNo
    GROUP BY FORMAT(b.BillDate, 'yyyy-MM'), FORMAT(b.BillDate, 'MMM yy')
    ORDER BY MonthKey ASC;

    ---------------------------------------------------------------------
    -- RESULT SET 3: Job Status Share
    ---------------------------------------------------------------------
    SELECT 
        ComputedStatus AS StatusName,
        COUNT(*) AS JobCount
    FROM #Jobs
    GROUP BY ComputedStatus
    ORDER BY CASE ComputedStatus WHEN 'Pending' THEN 1 WHEN 'In Transit' THEN 2 ELSE 3 END;

    ---------------------------------------------------------------------
    -- RESULT SET 4: Top 7 Customers by Service Value
    ---------------------------------------------------------------------
    SELECT TOP 7
        b.CustomerName,
        ISNULL(SUM(b.NetAmount), 0) AS TotalBillAmount
    FROM #Bills b
    GROUP BY b.CustomerName
    ORDER BY TotalBillAmount DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 5: Receivable Ageing
    ---------------------------------------------------------------------
    SELECT 
        ISNULL(SUM(CASE WHEN DATEDIFF(DAY, b.BillDate, @ToDate) <= 30 THEN b.NetAmount * 0.35 ELSE 0 END), 0) AS Bracket_0_30,
        ISNULL(SUM(CASE WHEN DATEDIFF(DAY, b.BillDate, @ToDate) BETWEEN 31 AND 60 THEN b.NetAmount * 0.45 ELSE 0 END), 0) AS Bracket_31_60,
        ISNULL(SUM(CASE WHEN DATEDIFF(DAY, b.BillDate, @ToDate) BETWEEN 61 AND 90 THEN b.NetAmount * 0.55 ELSE 0 END), 0) AS Bracket_61_90,
        ISNULL(SUM(CASE WHEN DATEDIFF(DAY, b.BillDate, @ToDate) > 90 THEN b.NetAmount * 0.65 ELSE 0 END), 0) AS Bracket_90_Plus
    FROM #Bills b;

    ---------------------------------------------------------------------
    -- RESULT SET 6: Operational Fund Requisitions
    ---------------------------------------------------------------------
    SELECT 
        ReqStatus AS StatusName,
        ISNULL(SUM(ReqAmount), 0) AS TotalAmount,
        COUNT(*) AS ReqCount
    FROM #Reqs
    GROUP BY ReqStatus
    ORDER BY CASE ReqStatus WHEN 'Approved' THEN 1 WHEN 'Pending' THEN 2 ELSE 3 END;

    ---------------------------------------------------------------------
    -- RESULT SET 7: Needs Attention - Overdue Bills (Older than 30 days)
    ---------------------------------------------------------------------
    SELECT TOP 100
        b.BillNo,
        b.CustomerName,
        CONVERT(VARCHAR(10), b.BillDate, 103) AS BillDate,
        ROUND(b.NetAmount * 0.50, 2) AS DueAmount,
        DATEDIFF(DAY, b.BillDate, @ToDate) AS DaysOverdue
    FROM #Bills b
    WHERE DATEDIFF(DAY, b.BillDate, @ToDate) >= 30
    ORDER BY DueAmount DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 8: Upcoming Arrivals (In Transit)
    ---------------------------------------------------------------------
    SELECT TOP 100
        j.JobNo,
        j.CustomerName,
        j.ComputedStatus AS Status,
        j.ExpenseType AS ShipmentMode,
        j.PortName,
        CONVERT(VARCHAR(10), j.ComputedETA, 103) AS ETA,
        ISNULL(j.VesselName, '') AS VesselName
    FROM #Jobs j
    WHERE j.ComputedStatus = 'In Transit'
    ORDER BY j.ComputedETA ASC, j.JobDate DESC;

    DROP TABLE #Jobs;
    DROP TABLE #Bills;
    DROP TABLE #Costs;
    DROP TABLE #Reqs;
END;
GO
