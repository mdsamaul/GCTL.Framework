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
    -- 1. Base Filtered Jobs
    ---------------------------------------------------------------------
    SELECT 
        d.JobNo,
        CAST(d.Date AS DATE) AS JobDate,
        d.ExpenseTypeID,
        ISNULL(et.ExpenseType, 'Other') AS ExpenseType,
        d.PortNameId,
        ISNULL(p.PortName, '') AS PortName,
        d.CustomerID,
        ISNULL(c.CustomerName, 'Unknown') AS CustomerName,
        d.InvoiceNo,
        ISNULL(d.InvoiceValue, 0) AS InvoiceValue,
        ISNULL(d.MaterialDescription, '') AS MaterialDescription,
        d.BENo,
        d.DocReceivedDate,
        d.DateOfClearence,
        d.ETA,
        CASE 
            WHEN d.DateOfClearence IS NOT NULL 
                 OR ISNULL(d.ShipmentStatus,'') LIKE '%DELIVER%' 
                 OR ISNULL(d.ShipmentStatus,'') LIKE '%SUCCESS%' THEN 'Delivered'
            WHEN d.ETA IS NOT NULL 
                 OR ISNULL(d.BENo,'') <> '' 
                 OR ISNULL(d.ShipmentStatus,'') LIKE '%TRANSIT%' 
                 OR ISNULL(d.ShipmentStatus,'') LIKE '%PROCESSING%' THEN 'In Transit'
            ELSE 'Pending'
        END AS ComputedStatus
    INTO #Jobs
    FROM Sep_Documentation d WITH (NOLOCK)
    LEFT JOIN CF_Def_ExpenseType et WITH (NOLOCK) ON et.ExpenseTypeID = d.ExpenseTypeID
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON c.CustomerID = d.CustomerID
    LEFT JOIN Sales_Def_PortName p WITH (NOLOCK) ON p.PortNameID = d.PortNameId
    WHERE (d.Date >= @FromDate AND d.Date <= DATEADD(DAY, 1, @ToDate))
      AND (@CustomerID = '' OR d.CustomerID = @CustomerID)
      AND (@ShipmentMode = '' OR d.ExpenseTypeID = @ShipmentMode)
      AND (@Port = '' OR d.PortNameId = @Port);

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

    DECLARE @AvgClearanceDays NUMERIC(18,1), @OnTimePct NUMERIC(18,1), @DocsPending INT;
    SELECT 
        @AvgClearanceDays = ISNULL(AVG(CAST(DATEDIFF(DAY, JobDate, DateOfClearence) AS NUMERIC(18,1))), 0),
        @OnTimePct = CASE WHEN COUNT(*) > 0 
                          THEN ROUND(SUM(CASE WHEN DATEDIFF(DAY, JobDate, DateOfClearence) <= 10 THEN 1.0 ELSE 0.0 END) * 100.0 / COUNT(*), 1)
                          ELSE 0 END
    FROM #Jobs
    WHERE DateOfClearence IS NOT NULL AND DateOfClearence >= JobDate;

    SELECT @DocsPending = COUNT(*) FROM #Jobs WHERE DocReceivedDate IS NULL;

    SELECT 
        ISNULL(@TotalJobs, 0) AS TotalJobs,
        ISNULL(@Pending, 0) AS PendingJobs,
        ISNULL(@Transit, 0) AS TransitJobs,
        ISNULL(@Delivered, 0) AS DeliveredJobs,
        ISNULL(@AvgClearanceDays, 0) AS AvgClearanceDays,
        ISNULL(@OnTimePct, 0) AS OnTimeDeliveryPct,
        ISNULL(@DocsPending, 0) AS DocsPendingCount,
        -- Upcoming in 7 days
        ISNULL(SUM(CASE WHEN ComputedStatus = 'In Transit' AND ETA IS NOT NULL AND DATEDIFF(DAY, GETDATE(), ETA) BETWEEN 0 AND 7 THEN 1 ELSE 0 END), 0) AS ArrivalsNext7Days,
        ISNULL(SUM(InvoiceValue), 0) AS TotalInvoiceValueUSD
    FROM #Jobs;

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
    -- RESULT SET 3: Jobs by Mode & Status (for stacked bar)
    ---------------------------------------------------------------------
    SELECT 
        ExpenseType AS ModeName,
        ComputedStatus AS StatusName,
        COUNT(*) AS JobCount
    FROM #Jobs
    GROUP BY ExpenseType, ComputedStatus
    ORDER BY ExpenseType, StatusName;

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
        SUM(CASE WHEN ComputedStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,
        SUM(CASE WHEN ComputedStatus = 'In Transit' THEN 1 ELSE 0 END) AS TransitCount,
        SUM(CASE WHEN ComputedStatus = 'Delivered' THEN 1 ELSE 0 END) AS DeliveredCount,
        COUNT(*) AS TotalCount
    FROM #Jobs
    GROUP BY FORMAT(JobDate, 'yyyy-MM'), FORMAT(JobDate, 'MMM yy')
    ORDER BY MonthKey ASC;

    ---------------------------------------------------------------------
    -- RESULT SET 6: Top Customers
    ---------------------------------------------------------------------
    SELECT TOP 7
        CustomerName,
        COUNT(*) AS JobCount
    FROM #Jobs
    GROUP BY CustomerName
    ORDER BY JobCount DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 7: Job List (Detailed Table)
    ---------------------------------------------------------------------
    SELECT 
        ROW_NUMBER() OVER (ORDER BY JobDate DESC, JobNo DESC) AS SL,
        JobNo,
        CONVERT(VARCHAR(10), JobDate, 103) AS JobDate,
        ExpenseType AS Mode,
        PortName,
        CustomerName,
        ISNULL(InvoiceNo, '') AS InvoiceNo,
        InvoiceValue,
        MaterialDescription,
        ISNULL(BENo, '') AS BENo,
        CONVERT(VARCHAR(10), ETA, 103) AS ETA,
        CONVERT(VARCHAR(10), DateOfClearence, 103) AS DeliveryDate,
        ComputedStatus AS Status
    FROM #Jobs
    ORDER BY JobDate DESC, JobNo DESC;

    DROP TABLE #Jobs;
END;
GO
