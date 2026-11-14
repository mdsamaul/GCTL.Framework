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
        b.IsCollectBill,
        b.MinimumAgencyCommission,
        b.OnInvoiceValue,
        b.LCValue,
        b.FOBValueBDT
    INTO #Bills
    FROM CF_BillEntry b WITH (NOLOCK)
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON c.CustomerID = b.CustomerID
    LEFT JOIN Sep_Documentation d WITH (NOLOCK) ON d.JobNo = b.JobNo
    LEFT JOIN HRM_EmployeeOfficialInfo emp WITH (NOLOCK) ON emp.EmployeeID = d.EmployeeId OR (LEN(d.EmployeeId) >= 4 AND emp.EmployeeID LIKE '%' + d.EmployeeId)
    WHERE (b.BillDate >= @FromDate AND b.BillDate <= DATEADD(DAY, 1, @ToDate))
      AND (@CustomerID = '' OR b.CustomerID = @CustomerID)
      AND (@ShipmentMode = '' OR b.ExpenseTypeID = @ShipmentMode OR d.ExpenseTypeID = @ShipmentMode)
      AND (@Port = '' OR d.PortNameId = @Port)
      AND (
          ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
          OR COALESCE(
              emp.BranchCode,
              CASE 
                  WHEN d.JobNo LIKE '%Ctg%' THEN '02'
                  WHEN d.JobNo LIKE '%Dha%' OR d.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                  WHEN d.JobNo LIKE '%Pangaon%' THEN '04'
                  ELSE '01'
              END
          ) = @BranchCode
      );

    CREATE CLUSTERED INDEX IX_Bills_BillNo ON #Bills(BillNo);
    CREATE INDEX IX_Bills_JobNo ON #Bills(JobNo);

    ---------------------------------------------------------------------
    -- 1b. Bill Payments & Due Amounts (Live from DB)
    ---------------------------------------------------------------------
    SELECT 
        prd.InvoiceNo,
        SUM(ISNULL(prd.Amount, 0)) AS CollectedAmount
    INTO #BillPayments
    FROM PaymentReceiveDetails prd WITH (NOLOCK)
    JOIN #Bills b ON b.BillNo = prd.InvoiceNo
    GROUP BY prd.InvoiceNo;

    CREATE CLUSTERED INDEX IX_BP_InvoiceNo ON #BillPayments(InvoiceNo);

    SELECT 
        b.BillNo,
        CASE 
            WHEN ISNULL(b.IsCollectBill, '') = 'Y' THEN b.NetAmount 
            ELSE ISNULL(bp.CollectedAmount, 0) 
        END AS CollectedAmount,
        CASE 
            WHEN ISNULL(b.IsCollectBill, '') = 'Y' THEN 0 
            ELSE CASE WHEN (b.NetAmount - ISNULL(bp.CollectedAmount, 0)) > 0 
                      THEN (b.NetAmount - ISNULL(bp.CollectedAmount, 0)) 
                      ELSE 0 END 
        END AS DueAmount
    INTO #BillDues
    FROM #Bills b
    LEFT JOIN #BillPayments bp ON bp.InvoiceNo = b.BillNo;

    CREATE CLUSTERED INDEX IX_BD_BillNo ON #BillDues(BillNo);

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
            WHEN LTRIM(RTRIM(ISNULL(d.ShipmentStatus, ''))) IN ('06', '14')
                 OR UPPER(LTRIM(RTRIM(ISNULL(d.ShipmentStatus, '')))) LIKE '%DELIV%'
                 OR UPPER(LTRIM(RTRIM(ISNULL(d.ShipmentStatus, '')))) LIKE '%DELEVER%'
                 OR UPPER(LTRIM(RTRIM(ISNULL(d.ShipmentStatus, '')))) LIKE '%DLIVER%'
                 OR UPPER(LTRIM(RTRIM(ISNULL(d.ShipmentStatus, '')))) LIKE '%SUCCESS%'
                 OR UPPER(LTRIM(RTRIM(ISNULL(d.ShipmentStatus, '')))) LIKE '%HANDOVER%'
                 OR UPPER(LTRIM(RTRIM(ISNULL(d.ShipmentStatus, '')))) = 'SHIPMENT DONE' THEN 'Delivered'
            WHEN LTRIM(RTRIM(ISNULL(d.ShipmentStatus, ''))) IN ('01','02','03','04','05','07','08','09','10','11','12','13')
                 OR UPPER(LTRIM(RTRIM(ISNULL(d.ShipmentStatus, '')))) LIKE '%TRANSIT%'
                 OR UPPER(LTRIM(RTRIM(ISNULL(d.ShipmentStatus, '')))) LIKE '%PROCESS%' THEN 'In Transit'
            ELSE 'Pending'
        END AS ComputedStatus
    INTO #Jobs
    FROM Sep_Documentation d WITH (NOLOCK)
    OUTER APPLY (
        SELECT TOP 1 emp.BranchCode
        FROM HRM_EmployeeOfficialInfo emp WITH (NOLOCK)
        WHERE emp.EmployeeID = d.EmployeeId OR (LEN(d.EmployeeId) >= 4 AND emp.EmployeeID LIKE '%' + d.EmployeeId)
    ) emp
    LEFT JOIN CF_Def_ExpenseType et WITH (NOLOCK) ON et.ExpenseTypeID = d.ExpenseTypeID
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON c.CustomerID = d.CustomerID
    LEFT JOIN Sales_Def_PortName p WITH (NOLOCK) ON p.PortNameID = d.PortNameId
    OUTER APPLY (
        SELECT TOP 1 b.BillNo, b.BillDate
        FROM #Bills b
        WHERE b.JobNo = d.JobNo
        ORDER BY b.BillDate DESC, b.BillNo DESC
    ) b
    WHERE (d.Date >= @FromDate AND d.Date < DATEADD(DAY, 1, @ToDate))
      AND (@CustomerID = '' OR d.CustomerID = @CustomerID)
      AND (@ShipmentMode = '' OR d.ExpenseTypeID = @ShipmentMode)
      AND (@Port = '' OR d.PortNameId = @Port)
      AND (
          ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
          OR COALESCE(
              emp.BranchCode,
              CASE 
                  WHEN d.JobNo LIKE '%Ctg%' THEN '02'
                  WHEN d.JobNo LIKE '%Dha%' OR d.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                  WHEN d.JobNo LIKE '%Pangaon%' THEN '04'
                  ELSE '01'
              END
          ) = @BranchCode
      );

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
    LEFT JOIN HRM_EmployeeOfficialInfo emp WITH (NOLOCK) ON emp.EmployeeID = d.EmployeeId OR (LEN(d.EmployeeId) >= 4 AND emp.EmployeeID LIKE '%' + d.EmployeeId)
    LEFT JOIN CF_OperationalFundRequisitionDetails reqd WITH (NOLOCK) ON reqd.OFRNo = req.OFRNo
    WHERE (req.OFRDate >= @FromDate AND req.OFRDate <= DATEADD(DAY, 1, @ToDate))
      AND (@CustomerID = '' OR d.CustomerID = @CustomerID)
      AND (@ShipmentMode = '' OR req.ExpenseTypeID = @ShipmentMode OR d.ExpenseTypeID = @ShipmentMode)
      AND (@Port = '' OR d.PortNameId = @Port)
      AND (
          ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
          OR COALESCE(
              emp.BranchCode,
              CASE 
                  WHEN d.JobNo LIKE '%Ctg%' THEN '02'
                  WHEN d.JobNo LIKE '%Dha%' OR d.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                  WHEN d.JobNo LIKE '%Pangaon%' THEN '04'
                  ELSE '01'
              END
          ) = @BranchCode
      )
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
          AND (@BranchCode = '' OR @BranchCode = 'ALL' OR ob.BranchCode = @BranchCode)
        GROUP BY CASE WHEN ob.GeneralLedgerCodeNo LIKE '10200400003%' THEN 'Bank' ELSE 'Cash' END
        
        UNION ALL
        
        SELECT 
            cd.AccType,
            SUM(CASE WHEN cd.TrType = 'Dr' THEN cd.DebitAmount ELSE -cd.CreditAmount END) AS NetBal
        FROM #CashDetails cd
        JOIN dbo.Acc_VoucherEntry ve WITH (NOLOCK) ON ve.VoucherEntryCodeNo = cd.VoucherEntryCodeNo
        WHERE ve.VoucherDate <= DATEADD(DAY, 1, @ToDate)
          AND ve.IsApproved IN ('Y', 'Posted')
          AND (@BranchCode = '' OR @BranchCode = 'ALL' OR ve.BranchCode = @BranchCode)
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

    -- Live Outstanding and Collection from #BillDues
    DECLARE @TotalDueOutstanding NUMERIC(18,2) = 0, @CollectedAmount NUMERIC(18,2) = 0;
    SELECT 
        @TotalDueOutstanding = ISNULL(SUM(DueAmount), 0),
        @CollectedAmount = ISNULL(SUM(CollectedAmount), 0)
    FROM #BillDues;

    DECLARE @CollectionRate NUMERIC(18,1) = CASE WHEN @NetServiceValue > 0 
                                                 THEN ROUND(@CollectedAmount * 100.0 / @NetServiceValue, 1) 
                                                 ELSE 0.0 END;

    -- Live Net Profit (Accounts): Income (Control 4xx) minus Expenses (Control 3xx)
    DECLARE @NetProfitAccounts NUMERIC(18,2) = 0;
    SELECT 
        @NetProfitAccounts = ISNULL(
            SUM(CASE WHEN LEFT(ved.AccCode, 1) = '4' THEN (ved.CreditAmount - ved.DebitAmount)
                     WHEN LEFT(ved.AccCode, 1) = '3' THEN -(ved.DebitAmount - ved.CreditAmount)
                     ELSE 0 END), 0)
    FROM dbo.Acc_VoucherEntryDetails ved WITH (NOLOCK)
    JOIN dbo.Acc_VoucherEntry ve WITH (NOLOCK) ON ve.VoucherEntryCodeNo = ved.VoucherEntryCodeNo
    WHERE ve.IsApproved IN ('Y', 'Posted')
      AND ve.VoucherDate >= @FromDate AND ve.VoucherDate <= DATEADD(DAY, 1, @ToDate)
      AND (@BranchCode = '' OR ve.BranchCode = @BranchCode);

    IF @NetProfitAccounts = 0 AND @GrossProfit > 0
    BEGIN
        SET @NetProfitAccounts = @GrossProfit;
    END

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
    -- RESULT SET 5: Receivable Ageing (Live Due Amounts)
    ---------------------------------------------------------------------
    SELECT 
        ISNULL(SUM(CASE WHEN DATEDIFF(DAY, b.BillDate, @ToDate) <= 30 THEN bd.DueAmount ELSE 0 END), 0) AS Bracket_0_30,
        ISNULL(SUM(CASE WHEN DATEDIFF(DAY, b.BillDate, @ToDate) BETWEEN 31 AND 60 THEN bd.DueAmount ELSE 0 END), 0) AS Bracket_31_60,
        ISNULL(SUM(CASE WHEN DATEDIFF(DAY, b.BillDate, @ToDate) BETWEEN 61 AND 90 THEN bd.DueAmount ELSE 0 END), 0) AS Bracket_61_90,
        ISNULL(SUM(CASE WHEN DATEDIFF(DAY, b.BillDate, @ToDate) > 90 THEN bd.DueAmount ELSE 0 END), 0) AS Bracket_90_Plus
    FROM #Bills b
    JOIN #BillDues bd ON bd.BillNo = b.BillNo
    WHERE bd.DueAmount > 0;

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
    -- RESULT SET 7: Needs Attention - Overdue Bills (Live Due, Older than 30 days)
    ---------------------------------------------------------------------
    SELECT TOP 100
        b.BillNo,
        b.CustomerName,
        CONVERT(VARCHAR(10), b.BillDate, 103) AS BillDate,
        ROUND(bd.DueAmount, 2) AS DueAmount,
        DATEDIFF(DAY, b.BillDate, @ToDate) AS DaysOverdue
    FROM #Bills b
    JOIN #BillDues bd ON bd.BillNo = b.BillNo
    WHERE DATEDIFF(DAY, b.BillDate, @ToDate) >= 30
      AND bd.DueAmount > 0
    ORDER BY bd.DueAmount DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 8: Upcoming Arrivals (In Transit, Sorted by latest/upcoming ETA)
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
    ORDER BY j.ComputedETA DESC, j.JobDate DESC;

    DROP TABLE #Jobs;
    DROP TABLE #Bills;
    DROP TABLE #BillPayments;
    DROP TABLE #BillDues;
    DROP TABLE #Costs;
    DROP TABLE #Reqs;
END;
GO