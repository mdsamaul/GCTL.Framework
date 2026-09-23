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

/****** Object:  StoredProcedure [dbo].[proc_CustomerJobSummery] ******/
CREATE OR ALTER PROCEDURE [dbo].[proc_CustomerJobSummery]
    @CustomerId nvarchar(50) = ''
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @TotalJob nvarchar(50),
            @PendingJob nvarchar(50),
            @TransitJob nvarchar(50),
            @DeliveredJob nvarchar(50);

    SET @TotalJob = (
        SELECT COUNT(JobNo) 
        FROM Sep_Documentation 
        WHERE (ISNULL(@CustomerId, '') = '' OR CustomerID = @CustomerId)
    );

    SET @PendingJob = (
        SELECT COUNT(JobNo) 
        FROM Sep_Documentation d
        WHERE (ISNULL(@CustomerId, '') = '' OR CustomerID = @CustomerId) 
          AND ISNULL(d.ShipmentStatus, '') = ''
    );

    SET @TransitJob = (
        SELECT COUNT(JobNo) 
        FROM Sep_Documentation d
        WHERE (ISNULL(@CustomerId, '') = '' OR CustomerID = @CustomerId) 
          AND ISNULL(d.ShipmentStatus, '') <> '' 
          AND ISNULL(d.ShipmentStatus, '') IN ('01','02','03','04','05','07','08','09','10','11','12','13')
    );

    SET @DeliveredJob = (
        SELECT COUNT(JobNo) 
        FROM Sep_Documentation d 
        WHERE (ISNULL(@CustomerId, '') = '' OR CustomerID = @CustomerId) 
          AND ISNULL(d.ShipmentStatus, '') IN ('06','14')
    );

    SELECT 
        ISNULL(@TotalJob, '0') AS TotalJob,
        ISNULL(@PendingJob, '0') AS PendingJob,
        ISNULL(@TransitJob, '0') AS TransitJob,
        ISNULL(@DeliveredJob, '0') AS DeliveredJob;
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

    SELECT * INTO #TempTable 
    FROM Sep_Documentation 
    WHERE (ISNULL(@CustomerId, '') = '' OR CustomerID = @CustomerId) 
      AND ISNULL(ShipmentStatus, '') = '';

    SELECT DISTINCT
        d.JobNo,
        CONVERT(varchar(50), d.Date, 103) AS JobDate,
        e.ExpenseType,
        p.PortName,
        CASE WHEN d.DocReceivedDate IS NOT NULL AND d.DocReceivedDate <> '1900-01-01' 
             THEN CONVERT(varchar(50), d.DocReceivedDate, 103) ELSE '' END AS DocReceivedDate,
        d.InvoiceNo,
        d.InvoiceValue,
        CONCAT(d.Quntity1, ' ', d.MaterialDescription) AS ContainerPkgs,
        '' AS Shipper,
        d.BillNo,
        'Pending' AS ShipmentStatus,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '07') AS EtdDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '08') AS FinalIgmRecDt,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '09') AS ShipArrivedDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '10') AS DischargeDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '11') AS DocsBankSignDate,
        ISNULL(d.BENo, '') AS BeNo,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '12') AS BeDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '13') AS EstimatedDeliveryDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '14') AS ActualDeliveryDate,
        d.Remarks,
        d.Term,
        CONCAT(d.VesselName, ' ', d.Vessel_RottNo) AS VesselName,
        d.ContainerNo,
        CONCAT(d.NameOfFreightForwarder, ' ', d.FreightForwarderPhoneNo) AS FreightForwarder
    INTO #TempTable2
    FROM #TempTable d
    LEFT JOIN Sales_Customer c ON d.CustomerID = c.CustomerID
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u1 ON d.Unit1 = u1.UnitTypID
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u2 ON d.Unit2 = u2.UnitTypID
    LEFT OUTER JOIN CF_Def_ExpenseType e ON d.ExpenseTypeID = e.ExpenseTypeID
    LEFT OUTER JOIN Sales_Def_PortName p ON d.PortNameId = p.PortNameId
    LEFT OUTER JOIN Sales_Def_PortType pt ON p.PortId = pt.PortId
    LEFT OUTER JOIN CA_Def_Currency curr ON d.CurrencyId = curr.CurrencyId 
    LEFT OUTER JOIN CF_BillEntry bill ON d.JobNo = bill.JobNo
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
      AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
      AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')));

    SELECT ROW_NUMBER() OVER (ORDER BY JobNo ASC) AS sl, * 
    FROM #TempTable2
    ORDER BY JobNo ASC;

    DROP TABLE #TempTable;
    DROP TABLE #TempTable2;
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

    SELECT * INTO #TempTable 
    FROM Sep_Documentation 
    WHERE (ISNULL(@CustomerId, '') = '' OR CustomerID = @CustomerId) 
      AND ISNULL(ShipmentStatus, '') IN ('07','08','09','10','11','12','13');

    SELECT DISTINCT
        d.JobNo,
        CONVERT(varchar(50), d.Date, 103) AS JobDate,
        e.ExpenseType,
        p.PortName,
        CASE WHEN d.DocReceivedDate IS NOT NULL AND d.DocReceivedDate <> '1900-01-01' 
             THEN CONVERT(varchar(50), d.DocReceivedDate, 103) ELSE '' END AS DocReceivedDate,
        d.InvoiceNo,
        d.InvoiceValue,
        CONCAT(d.Quntity1, ' ', d.MaterialDescription) AS ContainerPkgs,
        '' AS Shipper,
        d.BillNo,
        'In Transit' AS ShipmentStatus,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '07') AS EtdDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '08') AS FinalIgmRecDt,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '09') AS ShipArrivedDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '10') AS DischargeDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '11') AS DocsBankSignDate,
        ISNULL(d.BENo, '') AS BeNo,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '12') AS BeDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '13') AS EstimatedDeliveryDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '14') AS ActualDeliveryDate,
        d.Remarks,
        d.Term,
        CONCAT(d.VesselName, ' ', d.Vessel_RottNo) AS VesselName,
        d.ContainerNo,
        CONCAT(d.NameOfFreightForwarder, ' ', d.FreightForwarderPhoneNo) AS FreightForwarder
    INTO #TempTable2
    FROM #TempTable d
    LEFT JOIN Sales_Customer c ON d.CustomerID = c.CustomerID
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u1 ON d.Unit1 = u1.UnitTypID
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u2 ON d.Unit2 = u2.UnitTypID
    LEFT OUTER JOIN CF_Def_ExpenseType e ON d.ExpenseTypeID = e.ExpenseTypeID
    LEFT OUTER JOIN Sales_Def_PortName p ON d.PortNameId = p.PortNameId
    LEFT OUTER JOIN Sales_Def_PortType pt ON p.PortId = pt.PortId
    LEFT OUTER JOIN CA_Def_Currency curr ON d.CurrencyId = curr.CurrencyId 
    LEFT OUTER JOIN CF_BillEntry bill ON d.JobNo = bill.JobNo
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
      AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
      AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')));

    SELECT ROW_NUMBER() OVER (ORDER BY JobNo ASC) AS sl, * 
    FROM #TempTable2
    ORDER BY JobNo ASC;

    DROP TABLE #TempTable;
    DROP TABLE #TempTable2;
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

    SELECT * INTO #TempTable 
    FROM Sep_Documentation 
    WHERE (ISNULL(@CustomerId, '') = '' OR CustomerID = @CustomerId) 
      AND ISNULL(ShipmentStatus, '') = '14';

    SELECT DISTINCT
        d.JobNo,
        CONVERT(varchar(50), d.Date, 103) AS JobDate,
        e.ExpenseType,
        p.PortName,
        CASE WHEN d.DocReceivedDate IS NOT NULL AND d.DocReceivedDate <> '1900-01-01' 
             THEN CONVERT(varchar(50), d.DocReceivedDate, 103) ELSE '' END AS DocReceivedDate,
        d.InvoiceNo,
        d.InvoiceValue,
        CONCAT(d.Quntity1, ' ', d.MaterialDescription) AS ContainerPkgs,
        '' AS Shipper,
        d.BillNo,
        'Delivered' AS ShipmentStatus,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '07') AS EtdDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '08') AS FinalIgmRecDt,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '09') AS ShipArrivedDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '10') AS DischargeDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '11') AS DocsBankSignDate,
        ISNULL(d.BENo, '') AS BeNo,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '12') AS BeDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '13') AS EstimatedDeliveryDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '14') AS ActualDeliveryDate,
        d.Remarks,
        d.Term,
        CONCAT(d.VesselName, ' ', d.Vessel_RottNo) AS VesselName,
        d.ContainerNo,
        CONCAT(d.NameOfFreightForwarder, ' ', d.FreightForwarderPhoneNo) AS FreightForwarder
    INTO #TempTable2
    FROM #TempTable d
    LEFT JOIN Sales_Customer c ON d.CustomerID = c.CustomerID
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u1 ON d.Unit1 = u1.UnitTypID
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u2 ON d.Unit2 = u2.UnitTypID
    LEFT OUTER JOIN CF_Def_ExpenseType e ON d.ExpenseTypeID = e.ExpenseTypeID
    LEFT OUTER JOIN Sales_Def_PortName p ON d.PortNameId = p.PortNameId
    LEFT OUTER JOIN Sales_Def_PortType pt ON p.PortId = pt.PortId
    LEFT OUTER JOIN CA_Def_Currency curr ON d.CurrencyId = curr.CurrencyId 
    LEFT OUTER JOIN CF_BillEntry bill ON d.JobNo = bill.JobNo
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
      AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
      AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')));

    SELECT ROW_NUMBER() OVER (ORDER BY JobNo ASC) AS sl, * 
    FROM #TempTable2
    ORDER BY JobNo ASC;

    DROP TABLE #TempTable;
    DROP TABLE #TempTable2;
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

    SELECT DISTINCT * INTO #TempTable 
    FROM Sep_Documentation 
    WHERE (ISNULL(@CustomerId, '') = '' OR CustomerID = @CustomerId) 
      AND ISNULL(ShipmentStatus, '') = '';

    SELECT DISTINCT
        d.JobNo,
        CONVERT(varchar(50), d.Date, 103) AS JobDate,
        e.ExpenseType,
        p.PortName,
        CASE WHEN d.DocReceivedDate IS NOT NULL AND d.DocReceivedDate <> '1900-01-01' 
             THEN CONVERT(varchar(50), d.DocReceivedDate, 103) ELSE '' END AS DocReceivedDate,
        d.InvoiceNo,
        d.InvoiceValue,
        CONCAT(d.Quntity1, ' ', d.MaterialDescription) AS MaterialNamePkgsQty,
        '' AS ExpressNameCountry,
        d.LCNo AS LcNo,
        d.HAWB AS HawbNo,
        d.MAWB AS MawbNo,
        'Pending' AS ShipmentStatus,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '01') AS EtdDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '02') AS FlightActualDeliveryDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '03') AS ArrivalNoticeRcvdExpressDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '04') AS DocsBankSignDate,
        ISNULL(d.BENo, '') AS BeNo,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '05') AS BeDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '06') AS ActualDeliveryDate,
        d.Remarks
    INTO #TempTable2
    FROM #TempTable d
    LEFT JOIN Sales_Customer c ON d.CustomerID = c.CustomerID
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u1 ON d.Unit1 = u1.UnitTypID
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u2 ON d.Unit2 = u2.UnitTypID
    LEFT OUTER JOIN CF_Def_ExpenseType e ON d.ExpenseTypeID = e.ExpenseTypeID
    LEFT OUTER JOIN Sales_Def_PortName p ON d.PortNameId = p.PortNameId
    LEFT OUTER JOIN Sales_Def_PortType pt ON p.PortId = pt.PortId
    LEFT OUTER JOIN CA_Def_Currency curr ON d.CurrencyId = curr.CurrencyId 
    LEFT OUTER JOIN CF_BillEntry bill ON d.JobNo = bill.JobNo
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
      AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
      AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')));

    SELECT ROW_NUMBER() OVER (ORDER BY JobNo ASC) AS sl, * 
    FROM #TempTable2
    ORDER BY JobNo ASC;

    DROP TABLE #TempTable;
    DROP TABLE #TempTable2;
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

    SELECT * INTO #TempTable 
    FROM Sep_Documentation 
    WHERE (ISNULL(@CustomerId, '') = '' OR CustomerID = @CustomerId) 
      AND ISNULL(ShipmentStatus, '') IN ('01','02','03','04','05');

    SELECT DISTINCT
        d.JobNo,
        CONVERT(varchar(50), d.Date, 103) AS JobDate,
        e.ExpenseType,
        p.PortName,
        CASE WHEN d.DocReceivedDate IS NOT NULL AND d.DocReceivedDate <> '1900-01-01' 
             THEN CONVERT(varchar(50), d.DocReceivedDate, 103) ELSE '' END AS DocReceivedDate,
        d.InvoiceNo,
        d.InvoiceValue,
        CONCAT(d.Quntity1, ' ', d.MaterialDescription) AS MaterialNamePkgsQty,
        '' AS ExpressNameCountry,
        d.LCNo AS LcNo,
        d.HAWB AS HawbNo,
        d.MAWB AS MawbNo,
        'In Transit' AS ShipmentStatus,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '01') AS EtdDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '02') AS FlightActualDeliveryDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '03') AS ArrivalNoticeRcvdExpressDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '04') AS DocsBankSignDate,
        ISNULL(d.BENo, '') AS BeNo,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '05') AS BeDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '06') AS ActualDeliveryDate,
        d.Remarks
    INTO #TempTable2
    FROM #TempTable d
    LEFT JOIN Sales_Customer c ON d.CustomerID = c.CustomerID
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u1 ON d.Unit1 = u1.UnitTypID
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u2 ON d.Unit2 = u2.UnitTypID
    LEFT OUTER JOIN CF_Def_ExpenseType e ON d.ExpenseTypeID = e.ExpenseTypeID
    LEFT OUTER JOIN Sales_Def_PortName p ON d.PortNameId = p.PortNameId
    LEFT OUTER JOIN Sales_Def_PortType pt ON p.PortId = pt.PortId
    LEFT OUTER JOIN CA_Def_Currency curr ON d.CurrencyId = curr.CurrencyId 
    LEFT OUTER JOIN CF_BillEntry bill ON d.JobNo = bill.JobNo
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
      AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
      AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')));

    SELECT ROW_NUMBER() OVER (ORDER BY JobNo ASC) AS sl, * 
    FROM #TempTable2
    ORDER BY JobNo ASC;

    DROP TABLE #TempTable;
    DROP TABLE #TempTable2;
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

    SELECT * INTO #TempTable 
    FROM Sep_Documentation 
    WHERE (ISNULL(@CustomerId, '') = '' OR CustomerID = @CustomerId) 
      AND ISNULL(ShipmentStatus, '') = '06';

    SELECT DISTINCT
        d.JobNo,
        CONVERT(varchar(50), d.Date, 103) AS JobDate,
        e.ExpenseType,
        p.PortName,
        CASE WHEN d.DocReceivedDate IS NOT NULL AND d.DocReceivedDate <> '1900-01-01' 
             THEN CONVERT(varchar(50), d.DocReceivedDate, 103) ELSE '' END AS DocReceivedDate,
        d.InvoiceNo,
        d.InvoiceValue,
        CONCAT(d.Quntity1, ' ', d.MaterialDescription) AS MaterialNamePkgsQty,
        '' AS ExpressNameCountry,
        d.LCNo AS LcNo,
        d.HAWB AS HawbNo,
        d.MAWB AS MawbNo,
        'Delivered' AS ShipmentStatus,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '01') AS EtdDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '02') AS FlightActualDeliveryDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '03') AS ArrivalNoticeRcvdExpressDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '04') AS DocsBankSignDate,
        ISNULL(d.BENo, '') AS BeNo,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '05') AS BeDate,
        (SELECT CASE WHEN ISNULL(du.UpdateDateTime, '') <> '' AND du.UpdateDateTime <> '1900-01-01'
                     THEN FORMAT(du.UpdateDateTime, 'dd/MM/yyyy hh:mm tt') ELSE '' END
         FROM CF_DailyJobUpdateEntry du WHERE d.JobNo = du.JobNo AND du.UpdateStatus = '06') AS ActualDeliveryDate,
        d.Remarks
    INTO #TempTable2
    FROM #TempTable d
    LEFT JOIN Sales_Customer c ON d.CustomerID = c.CustomerID
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u1 ON d.Unit1 = u1.UnitTypID
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u2 ON d.Unit2 = u2.UnitTypID
    LEFT OUTER JOIN CF_Def_ExpenseType e ON d.ExpenseTypeID = e.ExpenseTypeID
    LEFT OUTER JOIN Sales_Def_PortName p ON d.PortNameId = p.PortNameId
    LEFT OUTER JOIN Sales_Def_PortType pt ON p.PortId = pt.PortId
    LEFT OUTER JOIN CA_Def_Currency curr ON d.CurrencyId = curr.CurrencyId 
    LEFT OUTER JOIN CF_BillEntry bill ON d.JobNo = bill.JobNo
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
      AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
      AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')));

    SELECT ROW_NUMBER() OVER (ORDER BY JobNo ASC) AS sl, * 
    FROM #TempTable2
    ORDER BY JobNo ASC;

    DROP TABLE #TempTable;
    DROP TABLE #TempTable2;
END
GO
