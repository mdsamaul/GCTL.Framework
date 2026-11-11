CREATE OR ALTER PROCEDURE dbo.SP_Dashboard_JobReconcile
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @BranchCode NVARCHAR(50) = '',
    @ShipmentMode NVARCHAR(50) = '',
    @Port NVARCHAR(50) = '',
    @CustomerID NVARCHAR(50) = '',
    @JobStatus NVARCHAR(50) = ''
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -3, CAST(GETDATE() AS DATE));
    IF @ToDate IS NULL SET @ToDate = CAST(GETDATE() AS DATE);
    SET @BranchCode = LTRIM(RTRIM(ISNULL(@BranchCode, '')));
    SET @ShipmentMode = LTRIM(RTRIM(ISNULL(@ShipmentMode, '')));
    SET @Port = LTRIM(RTRIM(ISNULL(@Port, '')));
    SET @CustomerID = LTRIM(RTRIM(ISNULL(@CustomerID, '')));
    SET @JobStatus = LTRIM(RTRIM(ISNULL(@JobStatus, '')));

    ---------------------------------------------------------------------
    -- 1. Filtered Jobs & Revenue
    ---------------------------------------------------------------------
    SELECT 
        be.JobNo,
        be.BillNo,
        CAST(be.BillDate AS DATE) AS BillDate,
        ISNULL(c.CustomerName, 'Unknown') AS CustomerName,
        d.CustomerID,
        d.ExpenseTypeID,
        d.CompanyCode,
        d.PortNameId,
        ISNULL(be.BillAmount, 0) AS Revenue
    INTO #Jobs
    FROM CF_BillEntry be WITH (NOLOCK)
    JOIN Sep_Documentation d WITH (NOLOCK) ON d.JobNo = be.JobNo
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON c.CustomerID = d.CustomerID
    LEFT JOIN HRM_Employee emp WITH (NOLOCK) 
        ON COALESCE(NULLIF(be.EmployeeId, ''), d.EmployeeId) = emp.EmployeeID 
        OR (LEN(COALESCE(NULLIF(be.EmployeeId, ''), d.EmployeeId)) >= 4 AND emp.EmployeeID LIKE '%' + COALESCE(NULLIF(be.EmployeeId, ''), d.EmployeeId))
    WHERE (be.BillDate >= @FromDate AND be.BillDate <= DATEADD(DAY, 1, @ToDate))
      AND (@CustomerID = '' OR d.CustomerID = @CustomerID)
      AND (@ShipmentMode = '' OR d.ExpenseTypeID = @ShipmentMode)
      AND (
          ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
          OR COALESCE(
              emp.BranchCode,
              CASE 
                  WHEN be.JobNo LIKE '%Ctg%' THEN '02'
                  WHEN be.JobNo LIKE '%Dha%' OR be.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                  WHEN be.JobNo LIKE '%Pangaon%' THEN '04'
                  ELSE '01'
              END
          ) = @BranchCode
      )
      AND (@Port = '' OR d.PortNameId = @Port)
      AND (
          @JobStatus = '' 
          OR d.ShipmentStatus = @JobStatus
          OR (
              CASE 
                  WHEN d.DateOfClearence IS NOT NULL 
                       OR ISNULL(d.ShipmentStatus,'') LIKE '%DELIVER%' 
                       OR ISNULL(d.ShipmentStatus,'') LIKE '%SUCCESS%' 
                       OR d.ShipmentStatus IN ('06', '14') THEN 'Delivered'
                  WHEN d.ETA IS NOT NULL 
                       OR ISNULL(d.BENo,'') <> '' 
                       OR ISNULL(d.ShipmentStatus,'') LIKE '%TRANSIT%' 
                       OR ISNULL(d.ShipmentStatus,'') LIKE '%PROCESSING%' 
                       OR d.ShipmentStatus IN ('01','02','03','04','05','07','08','09','10','11','12','13') THEN 'In Transit'
                  ELSE 'Pending'
              END
          ) = @JobStatus
      );

    CREATE CLUSTERED INDEX IX_Jobs_JobNo ON #Jobs(JobNo);

    ---------------------------------------------------------------------
    -- 2. Expense Head Aggregation per Job (Actual Approved Expense)
    ---------------------------------------------------------------------
    SELECT 
        j.JobNo,
        d.ExpenseHeadID,
        ISNULL(e.ExpenseHead, 'Other') AS ExpenseHead,
        SUM(ISNULL(COALESCE(NULLIF(d.AdjustApprovalAmount, 0), NULLIF(d.BillAdjustAmount, 0), d.ActualAmount, 0), 0)) AS HeadAmount
    INTO #JobHeads
    FROM #Jobs j
    INNER JOIN CF_OperationalFundRequisitionDetails d WITH (NOLOCK) ON d.JobNo = j.JobNo
    LEFT JOIN CF_Def_ExpenseHead e WITH (NOLOCK) ON e.ExpenseHeadID = d.ExpenseHeadID
    GROUP BY j.JobNo, d.ExpenseHeadID, e.ExpenseHead;

    CREATE CLUSTERED INDEX IX_JobHeads ON #JobHeads(JobNo, ExpenseHeadID);

    ---------------------------------------------------------------------
    -- 3. Total Expense per Job & GP
    ---------------------------------------------------------------------
    SELECT 
        j.JobNo,
        j.CustomerName,
        j.Revenue,
        ISNULL(h.TotalExpense, 0) AS TotalExpense,
        j.Revenue - ISNULL(h.TotalExpense, 0) AS Profit,
        CASE WHEN j.Revenue > 0 
             THEN ROUND((j.Revenue - ISNULL(h.TotalExpense, 0)) * 100.0 / j.Revenue, 2)
             ELSE 0 
        END AS GPPercent
    INTO #ReconRows
    FROM #Jobs j
    LEFT JOIN (
        SELECT JobNo, SUM(HeadAmount) AS TotalExpense
        FROM #JobHeads
        GROUP BY JobNo
    ) h ON h.JobNo = j.JobNo;

    ---------------------------------------------------------------------
    -- RESULT SET 1: KPI Summary & Minis
    ---------------------------------------------------------------------
    DECLARE @TotalBills INT, @NetBillValue NUMERIC(18,2), @TotalCosting NUMERIC(18,2), @GrossProfit NUMERIC(18,2), @GPMargin NUMERIC(18,2);
    DECLARE @AvgProfitPerJob NUMERIC(18,2), @AvgExpensePerJob NUMERIC(18,2);
    DECLARE @BestCustomer NVARCHAR(150), @BestCustomerGP NUMERIC(18,2);
    DECLARE @LowestMarginJob NVARCHAR(100), @LowestMarginGP NUMERIC(18,2);
    DECLARE @LargestExpenseHead NVARCHAR(150), @LargestExpenseHeadAmt NUMERIC(18,2);

    SELECT 
        @TotalBills = COUNT(*),
        @NetBillValue = ISNULL(SUM(Revenue), 0),
        @TotalCosting = ISNULL(SUM(TotalExpense), 0),
        @GrossProfit = ISNULL(SUM(Profit), 0),
        @GPMargin = CASE WHEN SUM(Revenue) > 0 THEN ROUND(SUM(Profit) * 100.0 / SUM(Revenue), 2) ELSE 0 END,
        @AvgProfitPerJob = CASE WHEN COUNT(*) > 0 THEN ISNULL(SUM(Profit), 0) / COUNT(*) ELSE 0 END,
        @AvgExpensePerJob = CASE WHEN COUNT(*) > 0 THEN ISNULL(SUM(TotalExpense), 0) / COUNT(*) ELSE 0 END
    FROM #ReconRows;

    -- Best Customer (GP%)
    ;WITH CustStats AS (
        SELECT 
            CustomerName,
            SUM(Revenue) AS TotRev,
            SUM(Profit) AS TotPr,
            CASE WHEN SUM(Revenue) > 0 THEN ROUND(SUM(Profit) * 100.0 / SUM(Revenue), 2) ELSE 0 END AS CustGP
        FROM #ReconRows
        GROUP BY CustomerName
        HAVING SUM(Revenue) > 0
    )
    SELECT TOP 1 @BestCustomer = CustomerName, @BestCustomerGP = CustGP FROM CustStats ORDER BY CustGP DESC;

    -- Lowest Margin Job
    SELECT TOP 1 
        @LowestMarginJob = JobNo, 
        @LowestMarginGP = GPPercent 
    FROM #ReconRows 
    ORDER BY GPPercent ASC;

    -- Largest Expense Head
    SELECT TOP 1 
        @LargestExpenseHead = ExpenseHead, 
        @LargestExpenseHeadAmt = SUM(HeadAmount)
    FROM #JobHeads
    GROUP BY ExpenseHead
    ORDER BY SUM(HeadAmount) DESC;

    SELECT 
        ISNULL(@TotalBills, 0) AS TotalBills,
        ISNULL(@NetBillValue, 0) AS NetBillValue,
        ISNULL(@TotalCosting, 0) AS TotalCosting,
        ISNULL(@GrossProfit, 0) AS GrossProfit,
        ISNULL(@GPMargin, 0) AS GPMargin,
        ISNULL(@AvgProfitPerJob, 0) AS AvgProfitPerJob,
        ISNULL(@AvgExpensePerJob, 0) AS AvgExpensePerJob,
        ISNULL(@BestCustomer, '-') AS BestCustomer,
        ISNULL(@BestCustomerGP, 0) AS BestCustomerGP,
        ISNULL(@LowestMarginJob, '-') AS LowestMarginJob,
        ISNULL(@LowestMarginGP, 0) AS LowestMarginGP,
        ISNULL(@LargestExpenseHead, '-') AS LargestExpenseHead,
        ISNULL(@LargestExpenseHeadAmt, 0) AS LargestExpenseHeadAmt;

    ---------------------------------------------------------------------
    -- RESULT SET 2: Top Expense Heads (Composition & Chart)
    ---------------------------------------------------------------------
    SELECT TOP 8
        ExpenseHead,
        SUM(HeadAmount) AS Amount
    FROM #JobHeads
    GROUP BY ExpenseHead
    ORDER BY Amount DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 3: Job GP% Distribution (Brackets)
    ---------------------------------------------------------------------
    SELECT 
        SUM(CASE WHEN GPPercent < 0 THEN 1 ELSE 0 END) AS Bracket_Neg,
        SUM(CASE WHEN GPPercent >= 0 AND GPPercent < 20 THEN 1 ELSE 0 END) AS Bracket_0_20,
        SUM(CASE WHEN GPPercent >= 20 AND GPPercent < 30 THEN 1 ELSE 0 END) AS Bracket_20_30,
        SUM(CASE WHEN GPPercent >= 30 AND GPPercent < 40 THEN 1 ELSE 0 END) AS Bracket_30_40,
        SUM(CASE WHEN GPPercent >= 40 AND GPPercent < 50 THEN 1 ELSE 0 END) AS Bracket_40_50,
        SUM(CASE WHEN GPPercent >= 50 THEN 1 ELSE 0 END) AS Bracket_50_Plus
    FROM #ReconRows;

    ---------------------------------------------------------------------
    -- RESULT SET 4: Customer-wise Revenue, Expense & Profit (Top 7)
    ---------------------------------------------------------------------
    SELECT TOP 7
        CustomerName,
        SUM(Revenue) AS Revenue,
        SUM(TotalExpense) AS Expense,
        SUM(Profit) AS Profit
    FROM #ReconRows
    GROUP BY CustomerName
    ORDER BY Revenue DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 5: Job Reconcile Main Table
    -- Identifies top 3 global expense heads to show as dedicated columns,
    -- with everything else falling into OtherExpenses
    ---------------------------------------------------------------------
    DECLARE @Head1 NVARCHAR(150), @Head2 NVARCHAR(150), @Head3 NVARCHAR(150);

    ;WITH Top3Heads AS (
        SELECT TOP 3 ExpenseHead, SUM(HeadAmount) AS TotalAmt
        FROM #JobHeads
        GROUP BY ExpenseHead
        ORDER BY TotalAmt DESC
    ), Ranked AS (
        SELECT ExpenseHead, ROW_NUMBER() OVER (ORDER BY TotalAmt DESC) AS Rnk
        FROM Top3Heads
    )
    SELECT 
        @Head1 = MAX(CASE WHEN Rnk = 1 THEN ExpenseHead END),
        @Head2 = MAX(CASE WHEN Rnk = 2 THEN ExpenseHead END),
        @Head3 = MAX(CASE WHEN Rnk = 3 THEN ExpenseHead END)
    FROM Ranked;

    SELECT 
        ROW_NUMBER() OVER (ORDER BY r.Revenue DESC) AS SN,
        r.JobNo,
        r.CustomerName,
        r.Revenue,
        ISNULL(h1.Amt, 0) AS Head1Amt,
        ISNULL(@Head1, 'Expense Head 1') AS Head1Name,
        ISNULL(h2.Amt, 0) AS Head2Amt,
        ISNULL(@Head2, 'Expense Head 2') AS Head2Name,
        ISNULL(h3.Amt, 0) AS Head3Amt,
        ISNULL(@Head3, 'Expense Head 3') AS Head3Name,
        r.TotalExpense - (ISNULL(h1.Amt, 0) + ISNULL(h2.Amt, 0) + ISNULL(h3.Amt, 0)) AS OtherExpenses,
        r.TotalExpense,
        r.Profit,
        r.GPPercent
    FROM #ReconRows r
    LEFT JOIN (SELECT JobNo, SUM(HeadAmount) AS Amt FROM #JobHeads WHERE ExpenseHead = @Head1 GROUP BY JobNo) h1 ON h1.JobNo = r.JobNo
    LEFT JOIN (SELECT JobNo, SUM(HeadAmount) AS Amt FROM #JobHeads WHERE ExpenseHead = @Head2 GROUP BY JobNo) h2 ON h2.JobNo = r.JobNo
    LEFT JOIN (SELECT JobNo, SUM(HeadAmount) AS Amt FROM #JobHeads WHERE ExpenseHead = @Head3 GROUP BY JobNo) h3 ON h3.JobNo = r.JobNo
    ORDER BY r.Revenue DESC;

    DROP TABLE #Jobs;
    DROP TABLE #JobHeads;
    DROP TABLE #ReconRows;
END;
GO
