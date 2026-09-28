USE [GCTL_ERP_DB]
GO
/****** Object:  UserDefinedFunction [dbo].[SplitString] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SplitString]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
    EXEC('CREATE FUNCTION [dbo].[SplitString] (@String NVARCHAR(MAX), @Delimiter CHAR(1) = '','') RETURNS TABLE AS RETURN (SELECT LTRIM(RTRIM(value)) AS Item FROM STRING_SPLIT(@String, @Delimiter) WHERE LTRIM(RTRIM(value)) <> '''')')
END
GO

-- Recommended Performance Index on CF_DailyJobUpdateEntry
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CF_DailyJobUpdateEntry_JobNo_Status' AND object_id = OBJECT_ID('CF_DailyJobUpdateEntry'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CF_DailyJobUpdateEntry_JobNo_Status 
    ON dbo.CF_DailyJobUpdateEntry (JobNo, UpdateStatus) 
    INCLUDE (UpdateDateTime);
END
GO

/****** Object:  StoredProcedure [dbo].[proc_CustomerJobSummery] ******/
CREATE OR ALTER PROCEDURE [dbo].[proc_CustomerJobSummery]
    @CustomerId nvarchar(50) = ''
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @TotalJob int,
            @PendingJob int,
            @TransitJob int,
            @DeliveredJob int;

    SELECT 
        @TotalJob = COUNT(JobNo),
        @PendingJob = SUM(CASE WHEN ISNULL(ShipmentStatus, '') = '' THEN 1 ELSE 0 END),
        @TransitJob = SUM(CASE WHEN ISNULL(ShipmentStatus, '') IN ('01','02','03','04','05','07','08','09','10','11','12','13') THEN 1 ELSE 0 END),
        @DeliveredJob = SUM(CASE WHEN ISNULL(ShipmentStatus, '') IN ('06','14') THEN 1 ELSE 0 END)
    FROM Sep_Documentation WITH (NOLOCK)
    WHERE (ISNULL(@CustomerId, '') = '' OR CustomerID = @CustomerId);

    SELECT 
        ISNULL(@TotalJob, 0) AS TotalJob,
        ISNULL(@PendingJob, 0) AS PendingJob,
        ISNULL(@TransitJob, 0) AS TransitJob,
        ISNULL(@DeliveredJob, 0) AS DeliveredJob;
END
GO

/****** Object:  StoredProcedure [dbo].[Proc_CustomerJobStatusPendingReportForSea] ******/
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusPendingReportForSea]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = ''
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH FilteredJobs AS (
        SELECT d.*,
               c.CustomerName,
               e.ExpenseType,
               p.PortName,
               curr.Symbol AS CurrencySymbol,
               curr.ShortName AS CurrencyName
        FROM Sep_Documentation d WITH (NOLOCK)
        LEFT JOIN Sales_Customer c WITH (NOLOCK) ON d.CustomerID = c.CustomerID
        LEFT OUTER JOIN CF_Def_ExpenseType e WITH (NOLOCK) ON d.ExpenseTypeID = e.ExpenseTypeID
        LEFT OUTER JOIN Sales_Def_PortName p WITH (NOLOCK) ON d.PortNameId = p.PortNameId
        LEFT OUTER JOIN CA_Def_Currency curr WITH (NOLOCK) ON d.CurrencyId = curr.CurrencyId
        WHERE (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId) 
          AND (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
          AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
          AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
          AND ISNULL(d.ShipmentStatus, '') = ''
    ),
    Updates AS (
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
        INNER JOIN FilteredJobs fj ON du.JobNo = fj.JobNo
        GROUP BY du.JobNo
    )
    SELECT 
        ROW_NUMBER() OVER (ORDER BY fj.JobNo ASC) AS sl,
        fj.JobNo,
        CONVERT(varchar(50), fj.Date, 103) AS JobDate,
        fj.ExpenseType,
        fj.PortName,
        CASE WHEN fj.DocReceivedDate IS NOT NULL AND fj.DocReceivedDate <> '1900-01-01' 
             THEN CONVERT(varchar(50), fj.DocReceivedDate, 103) ELSE '' END AS DocReceivedDate,
        fj.InvoiceNo,
        fj.InvoiceValue,
        ISNULL(fj.CurrencySymbol, '$') AS CurrencySymbol,
        ISNULL(fj.CurrencyName, 'USD') AS CurrencyName,
        fj.CurrencyId,
        CONCAT(fj.Quntity1, ' ', fj.MaterialDescription) AS ContainerPkgs,
        '' AS Shipper,
        fj.BillNo,
        'Pending' AS ShipmentStatus,
        CASE WHEN u.EtdDate IS NOT NULL AND u.EtdDate <> '1900-01-01' THEN CONVERT(varchar(50), u.EtdDate, 103) + ' ' + CONVERT(varchar(5), u.EtdDate, 108) ELSE '' END AS EtdDate,
        CASE WHEN u.FinalIgmRecDt IS NOT NULL AND u.FinalIgmRecDt <> '1900-01-01' THEN CONVERT(varchar(50), u.FinalIgmRecDt, 103) + ' ' + CONVERT(varchar(5), u.FinalIgmRecDt, 108) ELSE '' END AS FinalIgmRecDt,
        CASE WHEN u.ShipArrivedDate IS NOT NULL AND u.ShipArrivedDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ShipArrivedDate, 103) + ' ' + CONVERT(varchar(5), u.ShipArrivedDate, 108) ELSE '' END AS ShipArrivedDate,
        CASE WHEN u.DischargeDate IS NOT NULL AND u.DischargeDate <> '1900-01-01' THEN CONVERT(varchar(50), u.DischargeDate, 103) + ' ' + CONVERT(varchar(5), u.DischargeDate, 108) ELSE '' END AS DischargeDate,
        CASE WHEN u.DocsBankSignDate IS NOT NULL AND u.DocsBankSignDate <> '1900-01-01' THEN CONVERT(varchar(50), u.DocsBankSignDate, 103) + ' ' + CONVERT(varchar(5), u.DocsBankSignDate, 108) ELSE '' END AS DocsBankSignDate,
        ISNULL(fj.BENo, '') AS BeNo,
        CASE WHEN u.BeDate IS NOT NULL AND u.BeDate <> '1900-01-01' THEN CONVERT(varchar(50), u.BeDate, 103) + ' ' + CONVERT(varchar(5), u.BeDate, 108) ELSE '' END AS BeDate,
        CASE WHEN u.EstimatedDeliveryDate IS NOT NULL AND u.EstimatedDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.EstimatedDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.EstimatedDeliveryDate, 108) ELSE '' END AS EstimatedDeliveryDate,
        CASE WHEN u.ActualDeliveryDate IS NOT NULL AND u.ActualDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ActualDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.ActualDeliveryDate, 108) ELSE '' END AS ActualDeliveryDate,
        fj.Remarks,
        fj.Term,
        CONCAT(fj.VesselName, ' ', fj.Vessel_RottNo) AS VesselName,
        fj.ContainerNo,
        CONCAT(fj.NameOfFreightForwarder, ' ', fj.FreightForwarderPhoneNo) AS FreightForwarder,
        fj.CustomerID,
        ISNULL(fj.CustomerName, 'General Customer') AS CustomerName
    FROM FilteredJobs fj
    LEFT JOIN Updates u ON fj.JobNo = u.JobNo
    ORDER BY fj.JobNo ASC;
END
GO

/****** Object:  StoredProcedure [dbo].[Proc_CustomerJobStatusInTransitSea] ******/
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusInTransitSea]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = ''
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH FilteredJobs AS (
        SELECT d.*,
               c.CustomerName,
               e.ExpenseType,
               p.PortName,
               curr.Symbol AS CurrencySymbol,
               curr.ShortName AS CurrencyName
        FROM Sep_Documentation d WITH (NOLOCK)
        LEFT JOIN Sales_Customer c WITH (NOLOCK) ON d.CustomerID = c.CustomerID
        LEFT OUTER JOIN CF_Def_ExpenseType e WITH (NOLOCK) ON d.ExpenseTypeID = e.ExpenseTypeID
        LEFT OUTER JOIN Sales_Def_PortName p WITH (NOLOCK) ON d.PortNameId = p.PortNameId
        LEFT OUTER JOIN CA_Def_Currency curr WITH (NOLOCK) ON d.CurrencyId = curr.CurrencyId
        WHERE (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId) 
          AND (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
          AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
          AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
          AND ISNULL(d.ShipmentStatus, '') IN ('01','02','03','04','05','07','08','09','10','11','12','13')
    ),
    Updates AS (
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
        INNER JOIN FilteredJobs fj ON du.JobNo = fj.JobNo
        GROUP BY du.JobNo
    )
    SELECT 
        ROW_NUMBER() OVER (ORDER BY fj.JobNo ASC) AS sl,
        fj.JobNo,
        CONVERT(varchar(50), fj.Date, 103) AS JobDate,
        fj.ExpenseType,
        fj.PortName,
        CASE WHEN fj.DocReceivedDate IS NOT NULL AND fj.DocReceivedDate <> '1900-01-01' 
             THEN CONVERT(varchar(50), fj.DocReceivedDate, 103) ELSE '' END AS DocReceivedDate,
        fj.InvoiceNo,
        fj.InvoiceValue,
        ISNULL(fj.CurrencySymbol, '$') AS CurrencySymbol,
        ISNULL(fj.CurrencyName, 'USD') AS CurrencyName,
        fj.CurrencyId,
        CONCAT(fj.Quntity1, ' ', fj.MaterialDescription) AS ContainerPkgs,
        '' AS Shipper,
        fj.BillNo,
        'In Transit' AS ShipmentStatus,
        CASE WHEN u.EtdDate IS NOT NULL AND u.EtdDate <> '1900-01-01' THEN CONVERT(varchar(50), u.EtdDate, 103) + ' ' + CONVERT(varchar(5), u.EtdDate, 108) ELSE '' END AS EtdDate,
        CASE WHEN u.FinalIgmRecDt IS NOT NULL AND u.FinalIgmRecDt <> '1900-01-01' THEN CONVERT(varchar(50), u.FinalIgmRecDt, 103) + ' ' + CONVERT(varchar(5), u.FinalIgmRecDt, 108) ELSE '' END AS FinalIgmRecDt,
        CASE WHEN u.ShipArrivedDate IS NOT NULL AND u.ShipArrivedDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ShipArrivedDate, 103) + ' ' + CONVERT(varchar(5), u.ShipArrivedDate, 108) ELSE '' END AS ShipArrivedDate,
        CASE WHEN u.DischargeDate IS NOT NULL AND u.DischargeDate <> '1900-01-01' THEN CONVERT(varchar(50), u.DischargeDate, 103) + ' ' + CONVERT(varchar(5), u.DischargeDate, 108) ELSE '' END AS DischargeDate,
        CASE WHEN u.DocsBankSignDate IS NOT NULL AND u.DocsBankSignDate <> '1900-01-01' THEN CONVERT(varchar(50), u.DocsBankSignDate, 103) + ' ' + CONVERT(varchar(5), u.DocsBankSignDate, 108) ELSE '' END AS DocsBankSignDate,
        ISNULL(fj.BENo, '') AS BeNo,
        CASE WHEN u.BeDate IS NOT NULL AND u.BeDate <> '1900-01-01' THEN CONVERT(varchar(50), u.BeDate, 103) + ' ' + CONVERT(varchar(5), u.BeDate, 108) ELSE '' END AS BeDate,
        CASE WHEN u.EstimatedDeliveryDate IS NOT NULL AND u.EstimatedDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.EstimatedDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.EstimatedDeliveryDate, 108) ELSE '' END AS EstimatedDeliveryDate,
        CASE WHEN u.ActualDeliveryDate IS NOT NULL AND u.ActualDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ActualDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.ActualDeliveryDate, 108) ELSE '' END AS ActualDeliveryDate,
        fj.Remarks,
        fj.Term,
        CONCAT(fj.VesselName, ' ', fj.Vessel_RottNo) AS VesselName,
        fj.ContainerNo,
        CONCAT(fj.NameOfFreightForwarder, ' ', fj.FreightForwarderPhoneNo) AS FreightForwarder,
        fj.CustomerID,
        ISNULL(fj.CustomerName, 'General Customer') AS CustomerName
    FROM FilteredJobs fj
    LEFT JOIN Updates u ON fj.JobNo = u.JobNo
    ORDER BY fj.JobNo ASC;
END
GO

/****** Object:  StoredProcedure [dbo].[Proc_CustomerJobStatusDeliveredSea] ******/
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusDeliveredSea]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = ''
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH FilteredJobs AS (
        SELECT d.*,
               c.CustomerName,
               e.ExpenseType,
               p.PortName,
               curr.Symbol AS CurrencySymbol,
               curr.ShortName AS CurrencyName
        FROM Sep_Documentation d WITH (NOLOCK)
        LEFT JOIN Sales_Customer c WITH (NOLOCK) ON d.CustomerID = c.CustomerID
        LEFT OUTER JOIN CF_Def_ExpenseType e WITH (NOLOCK) ON d.ExpenseTypeID = e.ExpenseTypeID
        LEFT OUTER JOIN Sales_Def_PortName p WITH (NOLOCK) ON d.PortNameId = p.PortNameId
        LEFT OUTER JOIN CA_Def_Currency curr WITH (NOLOCK) ON d.CurrencyId = curr.CurrencyId
        WHERE (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId) 
          AND (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
          AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
          AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
          AND ISNULL(d.ShipmentStatus, '') = '14'
    ),
    Updates AS (
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
        INNER JOIN FilteredJobs fj ON du.JobNo = fj.JobNo
        GROUP BY du.JobNo
    )
    SELECT 
        ROW_NUMBER() OVER (ORDER BY fj.JobNo ASC) AS sl,
        fj.JobNo,
        CONVERT(varchar(50), fj.Date, 103) AS JobDate,
        fj.ExpenseType,
        fj.PortName,
        CASE WHEN fj.DocReceivedDate IS NOT NULL AND fj.DocReceivedDate <> '1900-01-01' 
             THEN CONVERT(varchar(50), fj.DocReceivedDate, 103) ELSE '' END AS DocReceivedDate,
        fj.InvoiceNo,
        fj.InvoiceValue,
        ISNULL(fj.CurrencySymbol, '$') AS CurrencySymbol,
        ISNULL(fj.CurrencyName, 'USD') AS CurrencyName,
        fj.CurrencyId,
        CONCAT(fj.Quntity1, ' ', fj.MaterialDescription) AS ContainerPkgs,
        '' AS Shipper,
        fj.BillNo,
        'Delivered' AS ShipmentStatus,
        CASE WHEN u.EtdDate IS NOT NULL AND u.EtdDate <> '1900-01-01' THEN CONVERT(varchar(50), u.EtdDate, 103) + ' ' + CONVERT(varchar(5), u.EtdDate, 108) ELSE '' END AS EtdDate,
        CASE WHEN u.FinalIgmRecDt IS NOT NULL AND u.FinalIgmRecDt <> '1900-01-01' THEN CONVERT(varchar(50), u.FinalIgmRecDt, 103) + ' ' + CONVERT(varchar(5), u.FinalIgmRecDt, 108) ELSE '' END AS FinalIgmRecDt,
        CASE WHEN u.ShipArrivedDate IS NOT NULL AND u.ShipArrivedDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ShipArrivedDate, 103) + ' ' + CONVERT(varchar(5), u.ShipArrivedDate, 108) ELSE '' END AS ShipArrivedDate,
        CASE WHEN u.DischargeDate IS NOT NULL AND u.DischargeDate <> '1900-01-01' THEN CONVERT(varchar(50), u.DischargeDate, 103) + ' ' + CONVERT(varchar(5), u.DischargeDate, 108) ELSE '' END AS DischargeDate,
        CASE WHEN u.DocsBankSignDate IS NOT NULL AND u.DocsBankSignDate <> '1900-01-01' THEN CONVERT(varchar(50), u.DocsBankSignDate, 103) + ' ' + CONVERT(varchar(5), u.DocsBankSignDate, 108) ELSE '' END AS DocsBankSignDate,
        ISNULL(fj.BENo, '') AS BeNo,
        CASE WHEN u.BeDate IS NOT NULL AND u.BeDate <> '1900-01-01' THEN CONVERT(varchar(50), u.BeDate, 103) + ' ' + CONVERT(varchar(5), u.BeDate, 108) ELSE '' END AS BeDate,
        CASE WHEN u.EstimatedDeliveryDate IS NOT NULL AND u.EstimatedDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.EstimatedDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.EstimatedDeliveryDate, 108) ELSE '' END AS EstimatedDeliveryDate,
        CASE WHEN u.ActualDeliveryDate IS NOT NULL AND u.ActualDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ActualDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.ActualDeliveryDate, 108) ELSE '' END AS ActualDeliveryDate,
        fj.Remarks,
        fj.Term,
        CONCAT(fj.VesselName, ' ', fj.Vessel_RottNo) AS VesselName,
        fj.ContainerNo,
        CONCAT(fj.NameOfFreightForwarder, ' ', fj.FreightForwarderPhoneNo) AS FreightForwarder,
        fj.CustomerID,
        ISNULL(fj.CustomerName, 'General Customer') AS CustomerName
    FROM FilteredJobs fj
    LEFT JOIN Updates u ON fj.JobNo = u.JobNo
    ORDER BY fj.JobNo ASC;
END
GO

/****** Object:  StoredProcedure [dbo].[Proc_CustomerJobStatusPendingExceptAll] (Air) ******/
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusPendingExceptAll]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = ''
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH FilteredJobs AS (
        SELECT d.*,
               c.CustomerName,
               e.ExpenseType,
               p.PortName,
               curr.Symbol AS CurrencySymbol,
               curr.ShortName AS CurrencyName
        FROM Sep_Documentation d WITH (NOLOCK)
        LEFT JOIN Sales_Customer c WITH (NOLOCK) ON d.CustomerID = c.CustomerID
        LEFT OUTER JOIN CF_Def_ExpenseType e WITH (NOLOCK) ON d.ExpenseTypeID = e.ExpenseTypeID
        LEFT OUTER JOIN Sales_Def_PortName p WITH (NOLOCK) ON d.PortNameId = p.PortNameId
        LEFT OUTER JOIN CA_Def_Currency curr WITH (NOLOCK) ON d.CurrencyId = curr.CurrencyId
        WHERE (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId) 
          AND (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
          AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
          AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
          AND ISNULL(d.ShipmentStatus, '') = ''
    ),
    Updates AS (
        SELECT 
            du.JobNo,
            MAX(CASE WHEN du.UpdateStatus = '01' THEN du.UpdateDateTime END) AS EtdDate,
            MAX(CASE WHEN du.UpdateStatus = '02' THEN du.UpdateDateTime END) AS FlightActualDeliveryDate,
            MAX(CASE WHEN du.UpdateStatus = '03' THEN du.UpdateDateTime END) AS ArrivalNoticeRcvdExpressDate,
            MAX(CASE WHEN du.UpdateStatus = '04' THEN du.UpdateDateTime END) AS DocsBankSignDate,
            MAX(CASE WHEN du.UpdateStatus = '05' THEN du.UpdateDateTime END) AS BeDate,
            MAX(CASE WHEN du.UpdateStatus = '06' THEN du.UpdateDateTime END) AS ActualDeliveryDate
        FROM CF_DailyJobUpdateEntry du WITH (NOLOCK)
        INNER JOIN FilteredJobs fj ON du.JobNo = fj.JobNo
        GROUP BY du.JobNo
    )
    SELECT 
        ROW_NUMBER() OVER (ORDER BY fj.JobNo ASC) AS sl,
        fj.JobNo,
        CONVERT(varchar(50), fj.Date, 103) AS JobDate,
        fj.ExpenseType,
        fj.PortName,
        CASE WHEN fj.DocReceivedDate IS NOT NULL AND fj.DocReceivedDate <> '1900-01-01' 
             THEN CONVERT(varchar(50), fj.DocReceivedDate, 103) ELSE '' END AS DocReceivedDate,
        fj.InvoiceNo,
        fj.InvoiceValue,
        ISNULL(fj.CurrencySymbol, '$') AS CurrencySymbol,
        ISNULL(fj.CurrencyName, 'USD') AS CurrencyName,
        fj.CurrencyId,
        CONCAT(fj.Quntity1, ' ', fj.MaterialDescription) AS MaterialNamePkgsQty,
        '' AS ExpressNameCountry,
        fj.LCNo AS LcNo,
        fj.HAWB AS HawbNo,
        fj.MAWB AS MawbNo,
        'Pending' AS ShipmentStatus,
        CASE WHEN u.EtdDate IS NOT NULL AND u.EtdDate <> '1900-01-01' THEN CONVERT(varchar(50), u.EtdDate, 103) + ' ' + CONVERT(varchar(5), u.EtdDate, 108) ELSE '' END AS EtdDate,
        CASE WHEN u.FlightActualDeliveryDate IS NOT NULL AND u.FlightActualDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.FlightActualDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.FlightActualDeliveryDate, 108) ELSE '' END AS FlightActualDeliveryDate,
        CASE WHEN u.ArrivalNoticeRcvdExpressDate IS NOT NULL AND u.ArrivalNoticeRcvdExpressDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ArrivalNoticeRcvdExpressDate, 103) + ' ' + CONVERT(varchar(5), u.ArrivalNoticeRcvdExpressDate, 108) ELSE '' END AS ArrivalNoticeRcvdExpressDate,
        CASE WHEN u.DocsBankSignDate IS NOT NULL AND u.DocsBankSignDate <> '1900-01-01' THEN CONVERT(varchar(50), u.DocsBankSignDate, 103) + ' ' + CONVERT(varchar(5), u.DocsBankSignDate, 108) ELSE '' END AS DocsBankSignDate,
        ISNULL(fj.BENo, '') AS BeNo,
        CASE WHEN u.BeDate IS NOT NULL AND u.BeDate <> '1900-01-01' THEN CONVERT(varchar(50), u.BeDate, 103) + ' ' + CONVERT(varchar(5), u.BeDate, 108) ELSE '' END AS BeDate,
        CASE WHEN u.ActualDeliveryDate IS NOT NULL AND u.ActualDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ActualDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.ActualDeliveryDate, 108) ELSE '' END AS ActualDeliveryDate,
        fj.Remarks,
        fj.CustomerID,
        ISNULL(fj.CustomerName, 'General Customer') AS CustomerName
    FROM FilteredJobs fj
    LEFT JOIN Updates u ON fj.JobNo = u.JobNo
    ORDER BY fj.JobNo ASC;
END
GO

/****** Object:  StoredProcedure [dbo].[Proc_CustomerJobStatusInTransitExceptAll] (Air) ******/
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusInTransitExceptAll]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = ''
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH FilteredJobs AS (
        SELECT d.*,
               c.CustomerName,
               e.ExpenseType,
               p.PortName,
               curr.Symbol AS CurrencySymbol,
               curr.ShortName AS CurrencyName
        FROM Sep_Documentation d WITH (NOLOCK)
        LEFT JOIN Sales_Customer c WITH (NOLOCK) ON d.CustomerID = c.CustomerID
        LEFT OUTER JOIN CF_Def_ExpenseType e WITH (NOLOCK) ON d.ExpenseTypeID = e.ExpenseTypeID
        LEFT OUTER JOIN Sales_Def_PortName p WITH (NOLOCK) ON d.PortNameId = p.PortNameId
        LEFT OUTER JOIN CA_Def_Currency curr WITH (NOLOCK) ON d.CurrencyId = curr.CurrencyId
        WHERE (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId) 
          AND (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
          AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
          AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
          AND ISNULL(d.ShipmentStatus, '') IN ('01','02','03','04','05')
    ),
    Updates AS (
        SELECT 
            du.JobNo,
            MAX(CASE WHEN du.UpdateStatus = '01' THEN du.UpdateDateTime END) AS EtdDate,
            MAX(CASE WHEN du.UpdateStatus = '02' THEN du.UpdateDateTime END) AS FlightActualDeliveryDate,
            MAX(CASE WHEN du.UpdateStatus = '03' THEN du.UpdateDateTime END) AS ArrivalNoticeRcvdExpressDate,
            MAX(CASE WHEN du.UpdateStatus = '04' THEN du.UpdateDateTime END) AS DocsBankSignDate,
            MAX(CASE WHEN du.UpdateStatus = '05' THEN du.UpdateDateTime END) AS BeDate,
            MAX(CASE WHEN du.UpdateStatus = '06' THEN du.UpdateDateTime END) AS ActualDeliveryDate
        FROM CF_DailyJobUpdateEntry du WITH (NOLOCK)
        INNER JOIN FilteredJobs fj ON du.JobNo = fj.JobNo
        GROUP BY du.JobNo
    )
    SELECT 
        ROW_NUMBER() OVER (ORDER BY fj.JobNo ASC) AS sl,
        fj.JobNo,
        CONVERT(varchar(50), fj.Date, 103) AS JobDate,
        fj.ExpenseType,
        fj.PortName,
        CASE WHEN fj.DocReceivedDate IS NOT NULL AND fj.DocReceivedDate <> '1900-01-01' 
             THEN CONVERT(varchar(50), fj.DocReceivedDate, 103) ELSE '' END AS DocReceivedDate,
        fj.InvoiceNo,
        fj.InvoiceValue,
        ISNULL(fj.CurrencySymbol, '$') AS CurrencySymbol,
        ISNULL(fj.CurrencyName, 'USD') AS CurrencyName,
        fj.CurrencyId,
        CONCAT(fj.Quntity1, ' ', fj.MaterialDescription) AS MaterialNamePkgsQty,
        '' AS ExpressNameCountry,
        fj.LCNo AS LcNo,
        fj.HAWB AS HawbNo,
        fj.MAWB AS MawbNo,
        'In Transit' AS ShipmentStatus,
        CASE WHEN u.EtdDate IS NOT NULL AND u.EtdDate <> '1900-01-01' THEN CONVERT(varchar(50), u.EtdDate, 103) + ' ' + CONVERT(varchar(5), u.EtdDate, 108) ELSE '' END AS EtdDate,
        CASE WHEN u.FlightActualDeliveryDate IS NOT NULL AND u.FlightActualDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.FlightActualDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.FlightActualDeliveryDate, 108) ELSE '' END AS FlightActualDeliveryDate,
        CASE WHEN u.ArrivalNoticeRcvdExpressDate IS NOT NULL AND u.ArrivalNoticeRcvdExpressDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ArrivalNoticeRcvdExpressDate, 103) + ' ' + CONVERT(varchar(5), u.ArrivalNoticeRcvdExpressDate, 108) ELSE '' END AS ArrivalNoticeRcvdExpressDate,
        CASE WHEN u.DocsBankSignDate IS NOT NULL AND u.DocsBankSignDate <> '1900-01-01' THEN CONVERT(varchar(50), u.DocsBankSignDate, 103) + ' ' + CONVERT(varchar(5), u.DocsBankSignDate, 108) ELSE '' END AS DocsBankSignDate,
        ISNULL(fj.BENo, '') AS BeNo,
        CASE WHEN u.BeDate IS NOT NULL AND u.BeDate <> '1900-01-01' THEN CONVERT(varchar(50), u.BeDate, 103) + ' ' + CONVERT(varchar(5), u.BeDate, 108) ELSE '' END AS BeDate,
        CASE WHEN u.ActualDeliveryDate IS NOT NULL AND u.ActualDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ActualDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.ActualDeliveryDate, 108) ELSE '' END AS ActualDeliveryDate,
        fj.Remarks,
        fj.CustomerID,
        ISNULL(fj.CustomerName, 'General Customer') AS CustomerName
    FROM FilteredJobs fj
    LEFT JOIN Updates u ON fj.JobNo = u.JobNo
    ORDER BY fj.JobNo ASC;
END
GO

/****** Object:  StoredProcedure [dbo].[Proc_CustomerJobStatusDeliveryExceptAll] (Air) ******/
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusDeliveryExceptAll]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = ''
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH FilteredJobs AS (
        SELECT d.*,
               c.CustomerName,
               e.ExpenseType,
               p.PortName,
               curr.Symbol AS CurrencySymbol,
               curr.ShortName AS CurrencyName
        FROM Sep_Documentation d WITH (NOLOCK)
        LEFT JOIN Sales_Customer c WITH (NOLOCK) ON d.CustomerID = c.CustomerID
        LEFT OUTER JOIN CF_Def_ExpenseType e WITH (NOLOCK) ON d.ExpenseTypeID = e.ExpenseTypeID
        LEFT OUTER JOIN Sales_Def_PortName p WITH (NOLOCK) ON d.PortNameId = p.PortNameId
        LEFT OUTER JOIN CA_Def_Currency curr WITH (NOLOCK) ON d.CurrencyId = curr.CurrencyId
        WHERE (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId) 
          AND (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
          AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
          AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
          AND ISNULL(d.ShipmentStatus, '') = '06'
    ),
    Updates AS (
        SELECT 
            du.JobNo,
            MAX(CASE WHEN du.UpdateStatus = '01' THEN du.UpdateDateTime END) AS EtdDate,
            MAX(CASE WHEN du.UpdateStatus = '02' THEN du.UpdateDateTime END) AS FlightActualDeliveryDate,
            MAX(CASE WHEN du.UpdateStatus = '03' THEN du.UpdateDateTime END) AS ArrivalNoticeRcvdExpressDate,
            MAX(CASE WHEN du.UpdateStatus = '04' THEN du.UpdateDateTime END) AS DocsBankSignDate,
            MAX(CASE WHEN du.UpdateStatus = '05' THEN du.UpdateDateTime END) AS BeDate,
            MAX(CASE WHEN du.UpdateStatus = '06' THEN du.UpdateDateTime END) AS ActualDeliveryDate
        FROM CF_DailyJobUpdateEntry du WITH (NOLOCK)
        INNER JOIN FilteredJobs fj ON du.JobNo = fj.JobNo
        GROUP BY du.JobNo
    )
    SELECT 
        ROW_NUMBER() OVER (ORDER BY fj.JobNo ASC) AS sl,
        fj.JobNo,
        CONVERT(varchar(50), fj.Date, 103) AS JobDate,
        fj.ExpenseType,
        fj.PortName,
        CASE WHEN fj.DocReceivedDate IS NOT NULL AND fj.DocReceivedDate <> '1900-01-01' 
             THEN CONVERT(varchar(50), fj.DocReceivedDate, 103) ELSE '' END AS DocReceivedDate,
        fj.InvoiceNo,
        fj.InvoiceValue,
        ISNULL(fj.CurrencySymbol, '$') AS CurrencySymbol,
        ISNULL(fj.CurrencyName, 'USD') AS CurrencyName,
        fj.CurrencyId,
        CONCAT(fj.Quntity1, ' ', fj.MaterialDescription) AS MaterialNamePkgsQty,
        '' AS ExpressNameCountry,
        fj.LCNo AS LcNo,
        fj.HAWB AS HawbNo,
        fj.MAWB AS MawbNo,
        'Delivered' AS ShipmentStatus,
        CASE WHEN u.EtdDate IS NOT NULL AND u.EtdDate <> '1900-01-01' THEN CONVERT(varchar(50), u.EtdDate, 103) + ' ' + CONVERT(varchar(5), u.EtdDate, 108) ELSE '' END AS EtdDate,
        CASE WHEN u.FlightActualDeliveryDate IS NOT NULL AND u.FlightActualDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.FlightActualDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.FlightActualDeliveryDate, 108) ELSE '' END AS FlightActualDeliveryDate,
        CASE WHEN u.ArrivalNoticeRcvdExpressDate IS NOT NULL AND u.ArrivalNoticeRcvdExpressDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ArrivalNoticeRcvdExpressDate, 103) + ' ' + CONVERT(varchar(5), u.ArrivalNoticeRcvdExpressDate, 108) ELSE '' END AS ArrivalNoticeRcvdExpressDate,
        CASE WHEN u.DocsBankSignDate IS NOT NULL AND u.DocsBankSignDate <> '1900-01-01' THEN CONVERT(varchar(50), u.DocsBankSignDate, 103) + ' ' + CONVERT(varchar(5), u.DocsBankSignDate, 108) ELSE '' END AS DocsBankSignDate,
        ISNULL(fj.BENo, '') AS BeNo,
        CASE WHEN u.BeDate IS NOT NULL AND u.BeDate <> '1900-01-01' THEN CONVERT(varchar(50), u.BeDate, 103) + ' ' + CONVERT(varchar(5), u.BeDate, 108) ELSE '' END AS BeDate,
        CASE WHEN u.ActualDeliveryDate IS NOT NULL AND u.ActualDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ActualDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.ActualDeliveryDate, 108) ELSE '' END AS ActualDeliveryDate,
        fj.Remarks,
        fj.CustomerID,
        ISNULL(fj.CustomerName, 'General Customer') AS CustomerName
    FROM FilteredJobs fj
    LEFT JOIN Updates u ON fj.JobNo = u.JobNo
    ORDER BY fj.JobNo ASC;
END
GO
