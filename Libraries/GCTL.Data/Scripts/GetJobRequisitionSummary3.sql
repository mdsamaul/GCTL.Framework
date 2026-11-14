SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =========================================================================================
-- Stored Procedure: dbo.GetJobRequisitionSummary3
-- Description: Retrieves Requisition Summary Report data grouped by Job.
--              Includes Receiptable, Non-Receiptable, and Total Cost per Job.
--              ULTRA-FAST & ALIGNED with C&F Dashboard Overview metrics.
-- =========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[GetJobRequisitionSummary3]
    @JobNoId       NVARCHAR(MAX) = '',
    @FromDate      DATE          = NULL,
    @ToDate        DATE          = NULL,
    @DateUse       NVARCHAR(50)  = 'N',
    @CustomerId    NVARCHAR(MAX) = '',
    @ExpenseTypeID NVARCHAR(MAX) = '',
    @PortId        NVARCHAR(MAX) = ''
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Parse Multi-Select Parameters into Indexed Temp Tables
    CREATE TABLE #SelectedJobs (JobNo NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    IF @JobNoId IS NOT NULL AND LTRIM(RTRIM(@JobNoId)) <> ''
    BEGIN
        INSERT INTO #SelectedJobs (JobNo)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@JobNoId, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END;

    CREATE TABLE #SelectedCustomers (CustomerId NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    IF @CustomerId IS NOT NULL AND LTRIM(RTRIM(@CustomerId)) <> ''
    BEGIN
        INSERT INTO #SelectedCustomers (CustomerId)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@CustomerId, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END;

    CREATE TABLE #SelectedShipmentModes (ShipmentModeId NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    IF @ExpenseTypeID IS NOT NULL AND LTRIM(RTRIM(@ExpenseTypeID)) <> ''
    BEGIN
        INSERT INTO #SelectedShipmentModes (ShipmentModeId)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@ExpenseTypeID, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END;

    CREATE TABLE #SelectedPorts (PortId NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    IF @PortId IS NOT NULL AND LTRIM(RTRIM(@PortId)) <> ''
    BEGIN
        INSERT INTO #SelectedPorts (PortId)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@PortId, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END;

    -- 2. Fast filter matched jobs from OFR Entry
    CREATE TABLE #MatchedJobs (JobNo NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    
    INSERT INTO #MatchedJobs (JobNo)
    SELECT DISTINCT fre.JobNo
    FROM CF_OperationalFundRequisitionEntry fre WITH (NOLOCK)
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND fre.OFRDate >= @FromDate AND fre.OFRDate <= @ToDate))
      AND (NOT EXISTS (SELECT 1 FROM #SelectedJobs) OR fre.JobNo IN (SELECT JobNo FROM #SelectedJobs));

    -- 3. Aggregate amounts per matched job
    SELECT 
        frd.JobNo,
        SUM(CASE 
            WHEN UPPER(LTRIM(RTRIM(COALESCE(eh.IsReceiptable, '')))) IN ('Y', 'YES', '1', 'TRUE') 
            THEN ISNULL(frd.ActualAmount, ISNULL(frd.EstimatedAmount, 0))
            ELSE 0 
        END) AS Receiptable,
        SUM(CASE 
            WHEN UPPER(LTRIM(RTRIM(COALESCE(eh.IsReceiptable, '')))) NOT IN ('Y', 'YES', '1', 'TRUE') OR eh.IsReceiptable IS NULL
            THEN ISNULL(frd.ActualAmount, ISNULL(frd.EstimatedAmount, 0))
            ELSE 0 
        END) AS NonReceiptable
    INTO #JobAmounts
    FROM #MatchedJobs mj
    INNER JOIN CF_OperationalFundRequisitionDetails frd WITH (NOLOCK) ON mj.JobNo = frd.JobNo
    LEFT JOIN CF_Def_ExpenseHead eh WITH (NOLOCK) ON frd.ExpenseHeadID = eh.ExpenseHeadID
    GROUP BY frd.JobNo;

    CREATE CLUSTERED INDEX IX_JobAmounts_JobNo ON #JobAmounts(JobNo);

    -- 4. Produce Final Grid Records
    SELECT 
        ROW_NUMBER() OVER (PARTITION BY COALESCE(NULLIF(cus.CustomerName, ''), du.CustomerId, 'Other') ORDER BY ja.JobNo) AS ROWNUMBER,
        COALESCE(NULLIF(cus.CustomerName, ''), du.CustomerId, 'Other') AS CustomerName,
        ja.JobNo,
        CONVERT(VARCHAR(10), COALESCE(du.[Date], GETDATE()), 103) AS JobDate,
        COALESCE(pt.PortType, 'Air') AS PortType,
        COALESCE(ext.ExpenseType, 'Air Import') AS ShipmentMode,
        ja.Receiptable,
        ja.NonReceiptable,
        (ja.Receiptable + ja.NonReceiptable) AS TotalCost,
        SUM(ja.Receiptable + ja.NonReceiptable) OVER () AS GrandTotal,
        SUM(ja.Receiptable + ja.NonReceiptable) OVER () AS DataColumn9,
        COUNT(1) OVER () AS TotalCount,
        COUNT(1) OVER () AS DataColumn10
    FROM #JobAmounts ja
    LEFT JOIN Sep_Documentation du WITH (NOLOCK) ON ja.JobNo = du.JobNo
    LEFT JOIN Sales_Customer cus WITH (NOLOCK) ON du.CustomerId = cus.CustomerID
    LEFT JOIN CF_Def_ExpenseType ext WITH (NOLOCK) ON du.ExpenseTypeID = ext.ExpenseTypeID
    LEFT JOIN Sales_Def_PortName pn WITH (NOLOCK) ON du.PortNameId = pn.PortNameId
    LEFT JOIN Sales_Def_PortType pt WITH (NOLOCK) ON pn.PortId = pt.PortId
    WHERE (NOT EXISTS (SELECT 1 FROM #SelectedCustomers) OR du.CustomerId IN (SELECT CustomerId FROM #SelectedCustomers))
      AND (NOT EXISTS (SELECT 1 FROM #SelectedShipmentModes) OR du.ExpenseTypeID IN (SELECT ShipmentModeId FROM #SelectedShipmentModes))
      AND (NOT EXISTS (SELECT 1 FROM #SelectedPorts) OR pn.PortId IN (SELECT PortId FROM #SelectedPorts))
    ORDER BY CustomerName, ja.JobNo
    OPTION (RECOMPILE);

    DROP TABLE #JobAmounts;
    DROP TABLE #MatchedJobs;
    DROP TABLE #SelectedJobs;
    DROP TABLE #SelectedCustomers;
    DROP TABLE #SelectedShipmentModes;
    DROP TABLE #SelectedPorts;
END;
GO
