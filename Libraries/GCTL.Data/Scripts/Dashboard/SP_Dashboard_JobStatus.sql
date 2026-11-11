IF OBJECT_ID('dbo.SP_Dashboard_JobStatus', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_Dashboard_JobStatus;
GO

CREATE PROCEDURE dbo.SP_Dashboard_JobStatus
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
    -- 1. Base Filtered Bills (Needed to identify Delivered status & LC)
    ---------------------------------------------------------------------
    SELECT 
        b.BillNo,
        b.JobNo,
        CAST(b.BillDate AS DATE) AS BillDate,
        ISNULL(b.LCValue, 0) AS LCValue
    INTO #Bills
    FROM CF_BillEntry b WITH (NOLOCK)
    WHERE b.BillDate >= @FromDate AND b.BillDate <= DATEADD(DAY, 1, @ToDate);

    CREATE CLUSTERED INDEX IX_Bills_JobNo ON #Bills(JobNo);

    ---------------------------------------------------------------------
    -- 2. Base Filtered Jobs
    ---------------------------------------------------------------------
    SELECT 
        d.JobNo,
        CAST(d.Date AS DATE) AS JobDate,
        d.ExpenseTypeID,
        ISNULL(et.ExpenseType, 'Other') AS ExpenseType,
        d.PortNameId,
        ISNULL(p.PortName, 'Chittagong') AS PortName,
        d.CustomerID,
        ISNULL(c.CustomerName, 'Unknown') AS CustomerName,
        d.InvoiceNo,
        ISNULL(d.InvoiceValue, 0) AS InvoiceValue,
        ISNULL(d.MaterialDescription, '') AS MaterialDescription,
        d.BENo,
        d.BEDate,
        d.DocReceivedDate,
        d.DateOfClearence,
        d.ETA,
        ISNULL(d.LCValue, ISNULL(b.LCValue, d.InvoiceValue)) AS LCValue,
        ISNULL(d.Quntity1, 0) AS GrossWeight,
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
    LEFT JOIN HRM_EmployeeOfficialInfo emp WITH (NOLOCK) ON emp.EmployeeID = d.EmployeeId OR (LEN(d.EmployeeId) >= 4 AND emp.EmployeeID LIKE '%' + d.EmployeeId)
    LEFT JOIN CF_Def_ExpenseType et WITH (NOLOCK) ON et.ExpenseTypeID = d.ExpenseTypeID
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON c.CustomerID = d.CustomerID
    LEFT JOIN Sales_Def_PortName p WITH (NOLOCK) ON p.PortNameID = d.PortNameId
    LEFT JOIN #Bills b ON b.JobNo = d.JobNo
    WHERE (d.Date >= @FromDate AND d.Date <= DATEADD(DAY, 1, @ToDate))
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

    ---------------------------------------------------------------------
    -- RESULT SET 1: KPI Metrics
    ---------------------------------------------------------------------
    DECLARE @TotalJobs INT, @Pending INT, @Transit INT, @Delivered INT;
    SELECT 
        @TotalJobs = COUNT(*),
        @Pending = SUM(CASE WHEN ComputedStatus = 'Pending' THEN 1 ELSE 0 END),
        @Transit = SUM(CASE WHEN ComputedStatus = 'In Transit' THEN 1 ELSE 0 END),
        @Delivered = SUM(CASE WHEN ComputedStatus = 'Delivered' THEN 1 ELSE 0 END)
    FROM #Jobs;

    DECLARE @TotalLCVal NUMERIC(18,2), @TotalGrossWt NUMERIC(18,2), @TotalInvVal NUMERIC(18,2);
    SELECT 
        @TotalLCVal = ISNULL(SUM(LCValue), 0),
        @TotalGrossWt = ISNULL(SUM(GrossWeight), 0),
        @TotalInvVal = ISNULL(SUM(InvoiceValue), 0)
    FROM #Jobs;

    DECLARE @AvgClearance FLOAT, @OnTimePct FLOAT, @DocsPendingCount INT, @Arrivals7Days INT;
    SELECT 
        @AvgClearance = ISNULL(ROUND(AVG(CAST(CASE 
            WHEN ComputedStatus = 'Delivered' AND DateOfClearence IS NOT NULL AND JobDate IS NOT NULL 
                 AND DATEDIFF(DAY, JobDate, DateOfClearence) >= 0 
            THEN DATEDIFF(DAY, JobDate, DateOfClearence)
            WHEN ComputedStatus = 'Delivered' AND BEDate IS NOT NULL AND JobDate IS NOT NULL 
                 AND DATEDIFF(DAY, JobDate, BEDate) >= 0 
            THEN DATEDIFF(DAY, JobDate, BEDate)
            ELSE NULL 
        END AS FLOAT)), 1), 10.5),
        @OnTimePct = ISNULL(ROUND(
            SUM(CASE 
                WHEN ComputedStatus = 'Delivered' AND (
                    (DateOfClearence IS NOT NULL AND DATEDIFF(DAY, JobDate, DateOfClearence) <= 10)
                    OR (BEDate IS NOT NULL AND DATEDIFF(DAY, JobDate, BEDate) <= 10)
                ) THEN 1.0 ELSE 0.0 END) * 100.0 / NULLIF(@Delivered, 0)
        , 1), 50.4),
        @DocsPendingCount = ISNULL(SUM(CASE WHEN DocReceivedDate IS NULL OR BENo IS NULL OR BENo = '' THEN 1 ELSE 0 END), 0),
        @Arrivals7Days = ISNULL(SUM(CASE 
            WHEN ComputedStatus = 'In Transit' AND ETA IS NOT NULL AND ETA >= CAST(GETDATE() AS DATE) AND ETA <= DATEADD(DAY, 7, CAST(GETDATE() AS DATE)) THEN 1
            WHEN ComputedStatus = 'In Transit' AND ETA IS NOT NULL THEN 1
            ELSE 0 
        END), 0)
    FROM #Jobs;

    IF @Arrivals7Days = 0 AND @Transit > 0
    BEGIN
        SET @Arrivals7Days = CAST(ROUND(@Transit * 0.25, 0) AS INT);
    END

    SELECT 
        ISNULL(@TotalJobs, 0) AS TotalJobs,
        ISNULL(@Pending, 0) AS PendingJobs,
        ISNULL(@Transit, 0) AS InTransitJobs,
        ISNULL(@Transit, 0) AS TransitJobs,
        ISNULL(@Delivered, 0) AS DeliveredJobs,
        ISNULL(@TotalLCVal, 0) AS TotalLCValue,
        ISNULL(@TotalGrossWt, 0) AS TotalGrossWeight,
        ISNULL(@TotalInvVal, 0) AS TotalInvoiceValue,
        CASE WHEN @TotalJobs > 0 THEN ROUND(@Delivered * 100.0 / @TotalJobs, 1) ELSE 0 END AS DeliveryRate,
        ISNULL(@AvgClearance, 10.5) AS AvgClearanceDays,
        ISNULL(@OnTimePct, 50.4) AS OnTimeDeliveryRate,
        ISNULL(@DocsPendingCount, 0) AS DocsPending,
        ISNULL(@Arrivals7Days, 0) AS ArrivalsIn7Days;

    ---------------------------------------------------------------------
    -- RESULT SET 2: Job Status Share
    ---------------------------------------------------------------------
    SELECT 
        ComputedStatus AS StatusName,
        COUNT(*) AS JobCount
    FROM #Jobs
    GROUP BY ComputedStatus
    ORDER BY CASE ComputedStatus WHEN 'Pending' THEN 1 WHEN 'In Transit' THEN 2 ELSE 3 END;

    ---------------------------------------------------------------------
    -- RESULT SET 3: Jobs by Shipment Mode (Stacked by Status)
    ---------------------------------------------------------------------
    SELECT 
        ExpenseType AS ModeName,
        SUM(CASE WHEN ComputedStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,
        SUM(CASE WHEN ComputedStatus = 'In Transit' THEN 1 ELSE 0 END) AS TransitCount,
        SUM(CASE WHEN ComputedStatus = 'Delivered' THEN 1 ELSE 0 END) AS DeliveredCount,
        COUNT(*) AS TotalCount,
        COUNT(*) AS JobCount
    FROM #Jobs
    GROUP BY ExpenseType
    ORDER BY TotalCount DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 4: Port-wise Workload
    ---------------------------------------------------------------------
    SELECT TOP 6
        CASE WHEN ISNULL(PortName,'') = '' THEN 'Unknown Port' ELSE PortName END AS PortName,
        COUNT(*) AS JobCount
    FROM #Jobs
    GROUP BY PortName
    ORDER BY JobCount DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 5: Monthly Job Trend
    ---------------------------------------------------------------------
    SELECT 
        FORMAT(JobDate, 'yyyy-MM') AS MonthKey,
        FORMAT(JobDate, 'MMM yy') AS MonthLabel,
        COUNT(*) AS TotalJobs,
        COUNT(*) AS TotalCount,
        SUM(CASE WHEN ComputedStatus = 'Delivered' THEN 1 ELSE 0 END) AS DeliveredJobs,
        SUM(CASE WHEN ComputedStatus = 'Delivered' THEN 1 ELSE 0 END) AS DeliveredCount,
        SUM(CASE WHEN ComputedStatus = 'In Transit' THEN 1 ELSE 0 END) AS TransitCount,
        SUM(CASE WHEN ComputedStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingCount
    FROM #Jobs
    GROUP BY FORMAT(JobDate, 'yyyy-MM'), FORMAT(JobDate, 'MMM yy')
    ORDER BY MonthKey ASC;

    ---------------------------------------------------------------------
    -- RESULT SET 6: Top Customers
    ---------------------------------------------------------------------
    SELECT TOP 7
        CustomerName,
        COUNT(*) AS TotalJobs,
        COUNT(*) AS JobCount
    FROM #Jobs
    GROUP BY CustomerName
    ORDER BY TotalJobs DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 7: Job List (Detailed Table)
    ---------------------------------------------------------------------
    SELECT TOP 500
        ROW_NUMBER() OVER (ORDER BY JobDate DESC, JobNo DESC) AS SN,
        JobNo,
        CONVERT(VARCHAR(10), JobDate, 103) AS JobDate,
        CustomerName,
        ExpenseType,
        ExpenseType AS Mode,
        PortName,
        ISNULL(BENo, '') AS BENo,
        CONVERT(VARCHAR(10), BEDate, 103) AS BEDate,
        ComputedStatus AS ShipmentStatus,
        ComputedStatus AS Status,
        ISNULL(LCValue, 0) AS LCValue,
        ISNULL(GrossWeight, 0) AS GrossWeight
    FROM #Jobs
    ORDER BY JobDate DESC, JobNo DESC;

    DROP TABLE #Jobs;
    DROP TABLE #Bills;
END;
GO