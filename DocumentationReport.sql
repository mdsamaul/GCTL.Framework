USE [GCTL_ERP_DB]
GO
/****** Object:  UserDefinedFunction [dbo].[SplitString] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER FUNCTION [dbo].[SplitString]
(
    @String NVARCHAR(MAX),
    @Delimiter CHAR(1) = ','
)
RETURNS TABLE
AS
RETURN
(
    SELECT LTRIM(RTRIM(value)) AS Item
    FROM STRING_SPLIT(@String, @Delimiter)
    WHERE LTRIM(RTRIM(value)) <> ''
);
GO

/****** Object:  StoredProcedure [dbo].[DocumentationReport]    Script Date: 9/22/2026 11:30:00 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[DocumentationReport]
    @DateUse       nvarchar(50) = 'N',
    @fromDate      nvarchar(50) = '',
    @toDate        nvarchar(50) = '',
    @CustomerID    nvarchar(50) = '',
    @PortId        nvarchar(50) = '',
    @ExpenseTypeID nvarchar(50) = '',
    @JobNo         nvarchar(MAX) = '',
    @JobStatus     nvarchar(50) = '',
    @ReportOption  nvarchar(50) = 'Job Report'
AS
BEGIN
    SET NOCOUNT ON;
    SET DATEFORMAT dmy;

    -- Safe date parsing handling dd/MM/yyyy (103), yyyy-MM-dd (120), or standard format
    DECLARE @parsedFromDate DATE = NULL;
    DECLARE @parsedToDate DATE = NULL;

    IF ISNULL(@fromDate, '') <> ''
    BEGIN
        IF @fromDate LIKE '[0-3][0-9]/[0-1][0-9]/[1-2][0-9][0-9][0-9]'
            SET @parsedFromDate = CONVERT(DATE, @fromDate, 103);
        ELSE IF @fromDate LIKE '[1-2][0-9][0-9][0-9]-[0-1][0-9]-[0-3][0-9]%'
            SET @parsedFromDate = CONVERT(DATE, @fromDate, 120);
        ELSE IF ISDATE(@fromDate) = 1
            SET @parsedFromDate = CAST(@fromDate AS DATE);
    END;

    IF ISNULL(@toDate, '') <> ''
    BEGIN
        IF @toDate LIKE '[0-3][0-9]/[0-1][0-9]/[1-2][0-9][0-9][0-9]'
            SET @parsedToDate = CONVERT(DATE, @toDate, 103);
        ELSE IF @toDate LIKE '[1-2][0-9][0-9][0-9]-[0-1][0-9]-[0-3][0-9]%'
            SET @parsedToDate = CONVERT(DATE, @toDate, 120);
        ELSE IF ISDATE(@toDate) = 1
            SET @parsedToDate = CAST(@toDate AS DATE);
    END;

    -- CTE to extract the single LATEST status update per JobNo from CF_DailyJobUpdateEntry.
    -- This eliminates Cartesian duplication caused by multiple daily update records.
    WITH LatestJobUpdate AS 
    (
        SELECT 
            dju.JobNo,
            dju.UpdateStatus,
            dju.UpdateRemarks,
            dju.UpdateDateTime,
            ROW_NUMBER() OVER (
                PARTITION BY dju.JobNo 
                ORDER BY dju.UpdateDateTime DESC, dju.TC DESC
            ) AS RowNum
        FROM CF_DailyJobUpdateEntry dju
    )
    SELECT DISTINCT
        d.JobNo,
        p.PrifixName,
        IIF(FORMAT(d.[Date], 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.[Date], 'dd/MM/yyyy')) AS JobDate,
        ctm.CustomerName,
        ctm.CustomerAddress,
        pn.PortName,
        IIF(FORMAT(d.DocReceivedDate, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.DocReceivedDate, 'dd/MM/yyyy')) AS DocReceivedDate,
        d.LCNo,
        d.BDTRate,
        -- Formatted Invoice / LC Value with Currency
        CASE 
            WHEN c.ShortName IS NOT NULL AND d.LCValue IS NOT NULL 
                THEN c.ShortName + ' ' + CONVERT(nvarchar(50), CAST(d.LCValue AS decimal(18, 2)))
            WHEN d.LCValue IS NOT NULL 
                THEN CONVERT(nvarchar(50), CAST(d.LCValue AS decimal(18, 2)))
            ELSE ''
        END AS LCValue,
        CAST(ISNULL(d.LCValue, 0) AS decimal(18, 2)) AS DataColumn1,
        ISNULL(c.ShortName, '') AS DataColumn2,
        d.IPNo,
        IIF(FORMAT(d.IPDate, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.IPDate, 'dd/MM/yyyy')) AS IPDate,
        d.UndertakingNo,
        d.InvoiceNo,
        COALESCE(NULLIF(d.BillNo, ''), d.BENo, '') AS BillNo,
        COALESCE(NULLIF(d.BillNo, ''), d.BENo, '') AS BLNo,
        d.BENo,
        IIF(FORMAT(d.BLDate, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.BLDate, 'dd/MM/yyyy')) AS BLDate,
        IIF(FORMAT(d.BEDate, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.BEDate, 'dd/MM/yyyy')) AS BEDate,
        s.SupplierName,
        s.SupplierName AS Shipper,
        d.PONo,
        d.ContainerNo,
        d.MaterialDescription,
        CONCAT(CAST(d.Quntity1 AS nvarchar(50)), ' ', u1.UnitTypeName) AS Quntity1,
        CONCAT(CAST(d.Quntity2 AS nvarchar(50)), ' ', u2.UnitTypeName) AS Quntity2,
        -- CONTAINER / PKGS: combines container number with package quantity and unit
        CASE 
            WHEN NULLIF(d.ContainerNo, '') IS NOT NULL AND d.PackingQuantity IS NOT NULL AND d.PackingQuantity > 0
                THEN d.ContainerNo + ' / ' + FORMAT(d.PackingQuantity, '0.##') + ' ' + ISNULL(d.PackingUnitType, '')
            WHEN NULLIF(d.ContainerNo, '') IS NOT NULL 
                THEN d.ContainerNo
            WHEN d.PackingQuantity IS NOT NULL AND d.PackingQuantity > 0
                THEN FORMAT(d.PackingQuantity, '0.##') + ' ' + ISNULL(d.PackingUnitType, '')
            WHEN d.Quntity1 IS NOT NULL AND d.Quntity1 > 0
                THEN FORMAT(d.Quntity1, '0.##') + ' ' + ISNULL(u1.UnitTypeName, '')
            ELSE ISNULL(d.ContainerNo, '')
        END AS ContainerPackages,
        d.FWDR_ShippingAgent,
        d.FWDR_ShippingAgent AS ForwarderName,
        d.Vessel_RottNo,
        d.Vessel_RottNo AS VesselRotNo,
        IIF(FORMAT(d.ETA, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.ETA, 'dd/MM/yyyy')) AS ETA,
        IIF(FORMAT(d.ETA, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.ETA, 'dd/MM/yyyy')) AS ETADelivery,
        IIF(FORMAT(d.ATA, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.ATA, 'dd/MM/yyyy')) AS ATA,
        IIF(FORMAT(d.ATB, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.ATB, 'dd/MM/yyyy')) AS ATB,
        IIF(FORMAT(d.ETB, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.ETB, 'dd/MM/yyyy')) AS ETB,
        IIF(FORMAT(d.Un_StuffingLocationDate, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.Un_StuffingLocationDate, 'dd/MM/yyyy')) AS Un_StuffingLocationDate,
        IIF(FORMAT(d.DateOfClearence, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.DateOfClearence, 'dd/MM/yyyy')) AS DateOfClearence,
        IIF(FORMAT(d.DateOfClearence, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.DateOfClearence, 'dd/MM/yyyy')) AS ActualDelivery,
        v.VehicleNo,
        d.Vehicle,
        d.Vendor,
        d.TransportCost,
        -- Readable Shipment Status from latest daily update or documentation table
        COALESCE(ssi.ShipmentStatus, lju.UpdateStatus, d.ShipmentStatus, '') AS ShipmentStatus,
        d.Remarks,
        d.DocumentationId,
        COALESCE(com.CompanyName, 'Formosa Logistics Limited') AS CompanyName,
        COALESCE(com.Address1, '21, Hazaribagh, Dhaka-1209') AS Address1,
        IIF(FORMAT(d.DateOfClearence, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.DateOfClearence, 'dd/MM/yyyy')) AS DeliveryDate,
        et.ExpenseType,
        ISNULL(@ReportOption, 'Job Report') AS DataColumn4,
        imp.CustomerName AS ImporterName,
        d.ContainerSize,
        COALESCE(d.LCANo, d.LCNo, '') AS LCANo,
        IIF(FORMAT(d.Dischargedate, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.Dischargedate, 'dd/MM/yyyy')) AS Dischargedate,
        IIF(FORMAT(d.Dischargedate, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.Dischargedate, 'dd/MM/yyyy')) AS DischargeDate,
        pol.PlaceOfLoadingName,
        pol.PlaceOfLoadingName AS LoadingPort,
        shy.ShedYardName,
        shy.ShedYardName AS ShedYard,
        d.FreightCharge,
        -- Docs Bank Sign (DocHandoverDate or DocsBankSignDate)
        IIF(FORMAT(COALESCE(d.DocsBankSignDate, d.DocumentHandoverDate), 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(COALESCE(d.DocsBankSignDate, d.DocumentHandoverDate), 'dd/MM/yyyy')) AS DocumentHandoverDate,
        IIF(FORMAT(COALESCE(d.DocsBankSignDate, d.DocumentHandoverDate), 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(COALESCE(d.DocsBankSignDate, d.DocumentHandoverDate), 'dd/MM/yyyy')) AS DocumentBankSign,
        IIF(FORMAT(d.CargoHandoverDate, 'dd/MM/yyyy') = '01/01/1900', '', FORMAT(d.CargoHandoverDate, 'dd/MM/yyyy')) AS CargoHandoverDate,
        ISNULL(d.DepotID, '') AS DepotName,
        d.BookingConfirmationReference,
        FORMAT(d.ETDDate, 'dd/MM/yyyy') AS ETDDate,
        FORMAT(d.ETACTGPORT, 'dd/MM/yyyy') AS ETACTGPORT,
        FORMAT(d.SHIPARRIVEDDATE, 'dd/MM/yyyy') AS SHIPARRIVEDDATE,
        -- Schema-missing fields mapped safely
        COALESCE(d.FreightCharge, '') AS Term,
        '' AS SurrenderedBLIssue
    FROM Sep_Documentation d  
    LEFT OUTER JOIN CF_Def_VendorPrefix p ON d.PrifixId = p.VendorPrifixId  
    LEFT OUTER JOIN RMG_Def_Supplier s ON d.SupplierId = s.SupplierId  
    LEFT OUTER JOIN Sales_Customer imp ON d.CustomerID_Importer = imp.CustomerID 
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u1 ON d.Unit1 = u1.UnitTypID  
    LEFT OUTER JOIN RMG_Prod_Def_UnitType u2 ON d.Unit2 = u2.UnitTypID  
    LEFT OUTER JOIN Sales_Customer ctm ON d.CustomerID = ctm.CustomerID  
    LEFT OUTER JOIN Sales_Def_PortName pn ON d.PortNameId = pn.PortNameId  
    LEFT OUTER JOIN Sales_Def_Vehicle v ON d.VehicleID = v.VehicleID  
    LEFT OUTER JOIN CA_Def_Currency c ON d.CurrencyId = c.CurrencyId  
    LEFT OUTER JOIN Core_Company com ON d.CompanyCode = com.CompanyCode  
    LEFT OUTER JOIN CF_Def_ExpenseType et ON d.ExpenseTypeID = et.ExpenseTypeID 
    LEFT OUTER JOIN CF_Def_PlaceOfLoading pol ON d.PlaceOfLoadingID = pol.PlaceOfLoadingID
    LEFT OUTER JOIN CF_Def_ShedYard shy ON d.ShedYardID = shy.ShedYardID
    -- Latest status CTE join (eliminates duplicate rows from multiple updates)
    LEFT OUTER JOIN LatestJobUpdate lju ON d.JobNo = lju.JobNo AND lju.RowNum = 1
    LEFT OUTER JOIN Sales_Def_ShipmentStatusInfo ssi ON lju.UpdateStatus = ssi.ShipmentStatusId
    WHERE 
        -- Date filter: only applied when @DateUse = 'Y'
        (
            @DateUse = 'N' 
            OR (
                @DateUse = 'Y' 
                AND (@parsedFromDate IS NULL OR CAST(d.[Date] AS DATE) >= @parsedFromDate)
                AND (@parsedToDate IS NULL OR CAST(d.[Date] AS DATE) <= @parsedToDate)
            )
        )
        -- Customer Filter
        AND (ISNULL(@CustomerID, '') = '' OR d.CustomerID = @CustomerID OR d.CustomerID IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@CustomerID, ',') WHERE LTRIM(RTRIM(value)) <> ''))
        -- Port Type Filter (checks port type via pn or d.PortId)
        AND (ISNULL(@PortId, '') = '' OR pn.PortId IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@PortId, ',') WHERE LTRIM(RTRIM(value)) <> '') OR d.PortId IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@PortId, ',') WHERE LTRIM(RTRIM(value)) <> ''))
        -- Shipment Mode Filter
        AND (ISNULL(@ExpenseTypeID, '') = '' OR d.ExpenseTypeID IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@ExpenseTypeID, ',') WHERE LTRIM(RTRIM(value)) <> ''))
        -- Job No Filter
        AND (ISNULL(@JobNo, '') = '' OR d.JobNo IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@JobNo, ',') WHERE LTRIM(RTRIM(value)) <> ''))
        -- Job Status Filter
        AND (
            ISNULL(@JobStatus, '') = '' 
            OR lju.UpdateStatus IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@JobStatus, ',') WHERE LTRIM(RTRIM(value)) <> '')
            OR ssi.ShipmentStatusId IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@JobStatus, ',') WHERE LTRIM(RTRIM(value)) <> '')
            OR ssi.ShipmentStatus IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@JobStatus, ',') WHERE LTRIM(RTRIM(value)) <> '')
            OR d.ShipmentStatus IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@JobStatus, ',') WHERE LTRIM(RTRIM(value)) <> '')
        )
    ORDER BY 
        ctm.CustomerName,
        et.ExpenseType,
        d.JobNo;
END
GO

