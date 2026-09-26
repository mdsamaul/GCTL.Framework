USE [GCTL_ERP_DB]
GO

-- =========================================================================
-- 1. PERFORMANCE INDEXES FOR MILLISECOND JOB STATUS DASHBOARD & REPORTS
-- =========================================================================

-- Index on CF_DailyJobUpdateEntry for instant Job Update status lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CF_DailyJobUpdateEntry_JobNo_Status' AND object_id = OBJECT_ID('CF_DailyJobUpdateEntry'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CF_DailyJobUpdateEntry_JobNo_Status
    ON [dbo].[CF_DailyJobUpdateEntry] ([JobNo], [UpdateStatus])
    INCLUDE ([UpdateDateTime]);
END
GO

-- Index on Sep_Documentation for fast customer and date filtering (without varchar(max) / text columns in keys)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sep_Documentation_JobStatus_Perf' AND object_id = OBJECT_ID('Sep_Documentation'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Sep_Documentation_JobStatus_Perf
    ON [dbo].[Sep_Documentation] ([CustomerID], [Date])
    INCLUDE ([JobNo], [ExpenseTypeID], [PortNameId], [InvoiceNo], [BillNo], [BENo], [DocReceivedDate]);
END
GO

-- Index on Sales_Customer for instant customer name and address retrieval
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_Customer_Id_Perf' AND object_id = OBJECT_ID('Sales_Customer'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Sales_Customer_Id_Perf
    ON [dbo].[Sales_Customer] ([CustomerID])
    INCLUDE ([CustomerName], [CustomerAddress]);
END
GO

-- Index on Sales_Def_PortName
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_Def_PortName_PortId' AND object_id = OBJECT_ID('Sales_Def_PortName'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Sales_Def_PortName_PortId
    ON [dbo].[Sales_Def_PortName] ([PortId])
    INCLUDE ([PortNameId], [PortName]);
END
GO

-- =========================================================================
-- 2. HIGH PERFORMANCE STORED PROCEDURES (SINGLE-PASS, NO CLR FORMAT, FAST JOIN)
-- =========================================================================

-- Sea Port Fast Procedure
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusFastSea]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = '',
    @JobStatus      nvarchar(50) = ''
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Updates AS (
        SELECT 
            du.JobNo,
            MAX(CASE WHEN du.UpdateStatus = '07' THEN du.UpdateDateTime END) AS EtdDate,
            MAX(CASE WHEN du.UpdateStatus = '08' THEN du.UpdateDateTime END) AS FinalIgmRecDt,
            MAX(CASE WHEN du.UpdateStatus = '09' THEN du.UpdateDateTime END) AS ShipArrivedDate,
            MAX(CASE WHEN du.UpdateStatus = '10' THEN du.UpdateDateTime END) AS DischargeDate,
            MAX(CASE WHEN du.UpdateStatus = '11' THEN du.UpdateDateTime END) AS DocsBankSignDate,
            MAX(CASE WHEN du.UpdateStatus = '12' THEN du.UpdateDateTime END) AS BeDate,
            MAX(CASE WHEN du.UpdateStatus = '13' THEN du.UpdateDateTime END) AS EstimatedDeliveryDate,
            MAX(CASE WHEN du.UpdateStatus = '14' THEN du.UpdateDateTime END) AS ActualDeliveryDate
        FROM CF_DailyJobUpdateEntry du WITH (NOLOCK)
        GROUP BY du.JobNo
    )
    SELECT 
        d.JobNo,
        d.Date AS RawJobDate,
        ISNULL(e.ExpenseType, 'Sea Freight') AS ExpenseType,
        ISNULL(p.PortName, '') AS PortName,
        d.DocReceivedDate AS RawDocReceivedDate,
        ISNULL(d.InvoiceNo, '') AS InvoiceNo,
        ISNULL(CONVERT(varchar(50), d.InvoiceValue), '') AS InvoiceValue,
        CONCAT(d.Quntity1, ' ', d.MaterialDescription) AS ContainerPkgs,
        '' AS Shipper,
        ISNULL(d.BillNo, '') AS BillNo,
        CASE 
            WHEN ISNULL(d.ShipmentStatus, '') = '14' THEN 'Delivered'
            WHEN ISNULL(d.ShipmentStatus, '') IN ('07','08','09','10','11','12','13') THEN 'In Transit'
            ELSE 'Pending'
        END AS ShipmentStatus,
        u.EtdDate,
        u.FinalIgmRecDt,
        u.ShipArrivedDate,
        u.DischargeDate,
        u.DocsBankSignDate,
        ISNULL(d.BENo, '') AS BeNo,
        u.BeDate,
        u.EstimatedDeliveryDate,
        u.ActualDeliveryDate,
        ISNULL(d.Remarks, '') AS Remarks,
        ISNULL(d.Term, '') AS Term,
        CONCAT(d.VesselName, ' ', d.Vessel_RottNo) AS VesselName,
        ISNULL(d.ContainerNo, '') AS ContainerNo,
        CONCAT(d.NameOfFreightForwarder, ' ', d.FreightForwarderPhoneNo) AS FreightForwarder,
        ISNULL(d.CustomerID, '') AS CustomerID,
        ISNULL(c.CustomerName, 'General Customer') AS CustomerName,
        ISNULL(c.CustomerAddress, '') AS CustomerAddress
    FROM Sep_Documentation d WITH (NOLOCK)
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON d.CustomerID = c.CustomerID
    LEFT OUTER JOIN CF_Def_ExpenseType e WITH (NOLOCK) ON d.ExpenseTypeID = e.ExpenseTypeID
    LEFT OUTER JOIN Sales_Def_PortName p WITH (NOLOCK) ON d.PortNameId = p.PortNameId
    LEFT OUTER JOIN Updates u ON d.JobNo = u.JobNo
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
      AND (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId)
      AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
      AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
      AND (
          ISNULL(@JobStatus, '') = '' 
          OR (@JobStatus = 'Pending' AND ISNULL(d.ShipmentStatus, '') = '')
          OR (@JobStatus IN ('In Transit', 'transit') AND ISNULL(d.ShipmentStatus, '') IN ('07','08','09','10','11','12','13'))
          OR (@JobStatus = 'Delivered' AND ISNULL(d.ShipmentStatus, '') = '14')
      )
      -- Exclude pure Air shipments if ExpenseType says Air
      AND (e.ExpenseType IS NULL OR e.ExpenseType NOT LIKE '%Air%')
    ORDER BY d.JobNo ASC;
END
GO

-- Air Port Fast Procedure
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusFastAir]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = '',
    @JobStatus      nvarchar(50) = ''
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Updates AS (
        SELECT 
            du.JobNo,
            MAX(CASE WHEN du.UpdateStatus = '01' THEN du.UpdateDateTime END) AS EtdDate,
            MAX(CASE WHEN du.UpdateStatus = '02' THEN du.UpdateDateTime END) AS FlightActualDeliveryDate,
            MAX(CASE WHEN du.UpdateStatus = '03' THEN du.UpdateDateTime END) AS ArrivalNoticeRcvdExpressDate,
            MAX(CASE WHEN du.UpdateStatus = '04' THEN du.UpdateDateTime END) AS DocsBankSignDate,
            MAX(CASE WHEN du.UpdateStatus = '05' THEN du.UpdateDateTime END) AS BeDate,
            MAX(CASE WHEN du.UpdateStatus = '06' THEN du.UpdateDateTime END) AS ActualDeliveryDate
        FROM CF_DailyJobUpdateEntry du WITH (NOLOCK)
        GROUP BY du.JobNo
    )
    SELECT 
        d.JobNo,
        d.Date AS RawJobDate,
        ISNULL(e.ExpenseType, 'Air Freight') AS ExpenseType,
        ISNULL(p.PortName, '') AS PortName,
        d.DocReceivedDate AS RawDocReceivedDate,
        ISNULL(d.InvoiceNo, '') AS InvoiceNo,
        ISNULL(CONVERT(varchar(50), d.InvoiceValue), '') AS InvoiceValue,
        CONCAT(d.Quntity1, ' ', d.MaterialDescription) AS MaterialNamePkgsQty,
        '' AS ExpressNameCountry,
        ISNULL(d.LCNo, '') AS LcNo,
        ISNULL(d.HAWB, '') AS HawbNo,
        ISNULL(d.MAWB, '') AS MawbNo,
        CASE 
            WHEN ISNULL(d.ShipmentStatus, '') = '06' THEN 'Delivered'
            WHEN ISNULL(d.ShipmentStatus, '') IN ('01','02','03','04','05') THEN 'In Transit'
            ELSE 'Pending'
        END AS ShipmentStatus,
        u.EtdDate,
        u.FlightActualDeliveryDate,
        u.ArrivalNoticeRcvdExpressDate,
        u.DocsBankSignDate,
        ISNULL(d.BENo, '') AS BeNo,
        u.BeDate,
        u.ActualDeliveryDate,
        ISNULL(d.Remarks, '') AS Remarks,
        ISNULL(d.CustomerID, '') AS CustomerID,
        ISNULL(c.CustomerName, 'General Customer') AS CustomerName,
        ISNULL(c.CustomerAddress, '') AS CustomerAddress
    FROM Sep_Documentation d WITH (NOLOCK)
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON d.CustomerID = c.CustomerID
    LEFT OUTER JOIN CF_Def_ExpenseType e WITH (NOLOCK) ON d.ExpenseTypeID = e.ExpenseTypeID
    LEFT OUTER JOIN Sales_Def_PortName p WITH (NOLOCK) ON d.PortNameId = p.PortNameId
    LEFT OUTER JOIN Updates u ON d.JobNo = u.JobNo
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
      AND (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId)
      AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
      AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
      AND (
          ISNULL(@JobStatus, '') = '' 
          OR (@JobStatus = 'Pending' AND ISNULL(d.ShipmentStatus, '') = '')
          OR (@JobStatus IN ('In Transit', 'transit') AND ISNULL(d.ShipmentStatus, '') IN ('01','02','03','04','05'))
          OR (@JobStatus = 'Delivered' AND ISNULL(d.ShipmentStatus, '') = '06')
      )
      -- Include if Air or has Air documents (HAWB, MAWB)
      AND (e.ExpenseType LIKE '%Air%' OR ISNULL(d.HAWB, '') <> '' OR ISNULL(d.MAWB, '') <> '')
    ORDER BY d.JobNo ASC;
END
GO
