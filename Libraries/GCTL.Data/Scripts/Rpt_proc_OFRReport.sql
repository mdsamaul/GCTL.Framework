-- =========================================================================================
-- Stored Procedure: dbo.Rpt_proc_OFRReport
-- Description: Retrieves Operational Fund Requisition (OFR) report data including Master
--              Job Information and Detail Expense breakdown. Supports multi-job selection,
--              dynamic Organization (Company) & Branch resolution, and date/status filtering.
-- =========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[Rpt_proc_OFRReport]
    @CompanyCode          VARCHAR(50)   = NULL,
    @BranchCode           VARCHAR(50)   = NULL,
    @FromDate             DATETIME      = NULL,
    @ToDate               DATETIME      = NULL,
    @ShipmentModeId       VARCHAR(500)  = NULL,
    @CustomerId           VARCHAR(500)  = NULL,
    @JobNo                NVARCHAR(MAX) = NULL,   -- Comma-delimited list of multiple Job Nos (e.g. 'JOB-01,JOB-02')
    @ServiceProvidedById  VARCHAR(500)  = NULL,
    @OFRReportType        VARCHAR(100)  = 'OFR Approval Report'
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Parse Multi-Select Parameters into Temp Tables
    CREATE TABLE #SelectedJobs (JobNo NVARCHAR(100) COLLATE DATABASE_DEFAULT);
    IF @JobNo IS NOT NULL AND LTRIM(RTRIM(@JobNo)) <> ''
    BEGIN
        INSERT INTO #SelectedJobs (JobNo)
        SELECT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@JobNo, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    CREATE TABLE #SelectedCustomers (CustomerId NVARCHAR(100) COLLATE DATABASE_DEFAULT);
    IF @CustomerId IS NOT NULL AND LTRIM(RTRIM(@CustomerId)) <> ''
    BEGIN
        INSERT INTO #SelectedCustomers (CustomerId)
        SELECT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@CustomerId, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    CREATE TABLE #SelectedShipmentModes (ShipmentModeId NVARCHAR(100) COLLATE DATABASE_DEFAULT);
    IF @ShipmentModeId IS NOT NULL AND LTRIM(RTRIM(@ShipmentModeId)) <> ''
    BEGIN
        INSERT INTO #SelectedShipmentModes (ShipmentModeId)
        SELECT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@ShipmentModeId, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    CREATE TABLE #SelectedServiceTypes (ServiceTypeId NVARCHAR(100) COLLATE DATABASE_DEFAULT);
    IF @ServiceProvidedById IS NOT NULL AND LTRIM(RTRIM(@ServiceProvidedById)) <> ''
    BEGIN
        INSERT INTO #SelectedServiceTypes (ServiceTypeId)
        SELECT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@ServiceProvidedById, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    -- 2. Resolve Dynamic Organization (Company) and Branch
    DECLARE @OrgName NVARCHAR(200);
    DECLARE @OrgAddress NVARCHAR(500);
    DECLARE @BranchName NVARCHAR(200);

    -- Try resolving company by @CompanyCode or first company in database
    SELECT TOP 1 
        @OrgName = CompanyName,
        @OrgAddress = COALESCE(NULLIF(LTRIM(RTRIM(Address1)), ''), LTRIM(RTRIM(Address2)), '21, Hazaribagh, Dhaka-1209')
    FROM Core_Company
    WHERE (@CompanyCode IS NOT NULL AND @CompanyCode <> '' AND CompanyCode = @CompanyCode)
       OR (@CompanyCode IS NULL OR @CompanyCode = '')
    ORDER BY CASE WHEN @CompanyCode IS NOT NULL AND @CompanyCode <> '' AND CompanyCode = @CompanyCode THEN 0 ELSE 1 END;

    IF @OrgName IS NULL OR @OrgName = ''
    BEGIN
        SET @OrgName = 'Formosa Logistics Limited';
        SET @OrgAddress = '21, Hazaribagh, Dhaka-1209';
    END

    -- Try resolving branch by @BranchCode
    IF @BranchCode IS NOT NULL AND @BranchCode <> '' AND UPPER(@BranchCode) <> 'ALL'
    BEGIN
        SELECT TOP 1 
            @BranchName = BranchName,
            @OrgAddress = COALESCE(NULLIF(LTRIM(RTRIM(Address)), ''), @OrgAddress)
        FROM Core_Branch
        WHERE BranchCode = @BranchCode;
    END

    IF @BranchName IS NULL
    BEGIN
        SET @BranchName = '';
    END

    -- 3. Retrieve Master OFR Data into #MasterRecords
    SELECT 
        ofr.OFRNo,
        ofr.JobNo,
        COALESCE(doc.CustomerId, '') AS CustomerId,
        COALESCE(NULLIF(cust.CustomerName, ''), doc.CustomerId, '') AS CustomerName,
        CASE 
            WHEN doc.Quntity1 IS NOT NULL THEN CONCAT(CONVERT(VARCHAR(50), doc.Quntity1), ' ', COALESCE(doc.Unit1, 'PKG'))
            ELSE '1.00 PKG'
        END AS Quantity,
        COALESCE(NULLIF(doc.InvoiceNo, ''), ofr.OFRNo, '') AS InvoiceNo,
        CONVERT(VARCHAR(10), COALESCE(ofr.OFRDate, doc.[Date], GETDATE()), 103) AS [Date],
        ISNULL(CONVERT(VARCHAR(50), doc.Lcvalue), '0.00') AS LCValue,
        COALESCE(NULLIF(doc.Hawb, ''), NULLIF(doc.Mawb, ''), NULLIF(doc.Beno, ''), NULLIF(doc.BillNo, ''), '') AS BLNo,
        CASE 
            WHEN doc.Quntity2 IS NOT NULL THEN CONCAT(CONVERT(VARCHAR(50), doc.Quntity2), COALESCE(doc.Unit2, 'KG'))
            ELSE '36.00KG'
        END AS [Weight],
        COALESCE(expType.ExpenseType, 'Air Import') AS ShipmentMode,
        COALESCE(comp.CompanyName, @OrgName) AS OrganizationName,
        COALESCE(br.BranchName, @BranchName) AS BranchName,
        COALESCE(NULLIF(LTRIM(RTRIM(br.Address)), ''), NULLIF(LTRIM(RTRIM(comp.Address1)), ''), @OrgAddress) AS CompanyAddress,
        @OFRReportType AS OFRReportType,
        COALESCE(NULLIF(ofr.ApprovedUser, ''), 'shariful Islam') AS ApproveBy,
        COALESCE(NULLIF(ofr.ApprovedUser, ''), 'shariful Islam') AS CustomUser,
        CONVERT(VARCHAR(30), GETDATE(), 103) + ' ' + RIGHT(CONVERT(VARCHAR(30), GETDATE(), 100), 7) AS PrintDateTime
    INTO #MasterRecords
    FROM CF_OperationalFundRequisitionEntry ofr
    LEFT JOIN Sep_Documentation doc ON ofr.JobNo = doc.JobNo
    LEFT JOIN Sales_Customer cust ON doc.CustomerId = cust.CustomerId
    LEFT JOIN CF_Def_ExpenseType expType ON doc.ExpenseTypeId = expType.ExpenseTypeId
    LEFT JOIN Core_Company comp ON COALESCE(ofr.CompanyCode, doc.CompanyCode) = comp.CompanyCode
    OUTER APPLY (
        SELECT TOP 1 BranchName, Address 
        FROM Core_Branch 
        WHERE CompanyCode = COALESCE(ofr.CompanyCode, doc.CompanyCode)
        ORDER BY BranchCode
    ) br
    WHERE (NOT EXISTS (SELECT 1 FROM #SelectedJobs) OR ofr.JobNo IN (SELECT JobNo FROM #SelectedJobs))
      AND (NOT EXISTS (SELECT 1 FROM #SelectedCustomers) OR doc.CustomerId IN (SELECT CustomerId FROM #SelectedCustomers))
      AND (NOT EXISTS (SELECT 1 FROM #SelectedShipmentModes) OR doc.ExpenseTypeId IN (SELECT ShipmentModeId FROM #SelectedShipmentModes))
      AND (@CompanyCode IS NULL OR @CompanyCode = '' OR ofr.CompanyCode = @CompanyCode OR doc.CompanyCode = @CompanyCode)
      AND (@FromDate IS NULL OR COALESCE(ofr.OFRDate, doc.[Date]) >= @FromDate)
      AND (@ToDate IS NULL OR COALESCE(ofr.OFRDate, doc.[Date]) <= DATEADD(SECOND, -1, DATEADD(DAY, 1, @ToDate)))
      AND (
          @OFRReportType IS NULL OR @OFRReportType = ''
          OR (@OFRReportType = 'OFR Report')
          OR (@OFRReportType = 'OFR Approval Report' AND ofr.ApprovedUser IS NOT NULL AND LTRIM(RTRIM(ofr.ApprovedUser)) <> '')
          OR (@OFRReportType = 'OFR Final Report' AND ofr.BillAdjustApprovalUser IS NOT NULL AND LTRIM(RTRIM(ofr.BillAdjustApprovalUser)) <> '')
      )
    ORDER BY ofr.OFRDate DESC, ofr.TC DESC;

    -- Return 1st Result Set: Master Records
    SELECT * FROM #MasterRecords;

    -- Return 2nd Result Set: Detail Expense Records for the matched jobs
    SELECT 
        det.TC,
        det.OFRNo,
        det.JobNo,
        det.ExpenseHeadId,
        COALESCE(head.ExpenseHead, det.ExpenseHeadId, 'Expense') AS ExpenseHead,
        ISNULL(det.EstimatedAmount, 0) AS RequisitionAmount,
        ISNULL(det.ActualAmount, ISNULL(det.EstimatedAmount, 0)) AS ApprovalAmount,
        ISNULL(det.BillAdjustAmount, ISNULL(det.ActualAmount, ISNULL(det.EstimatedAmount, 0))) AS FinalAmount,
        CASE 
            WHEN UPPER(LTRIM(RTRIM(COALESCE(head.IsReceiptable, '')))) IN ('Y', 'YES', '1', 'TRUE') THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END AS IsReceiptable,
        det.ServiceTypeId,
        COALESCE(st.ServiceTypeName, 'Custom') AS ServiceTypeName,
        det.ExpenseHeadRemarks AS Remarks
    FROM CF_OperationalFundRequisitionDetails det
    INNER JOIN #MasterRecords m ON det.JobNo = m.JobNo
    LEFT JOIN CF_Def_ExpenseHead head ON det.ExpenseHeadId = head.ExpenseHeadId
    LEFT JOIN Core_ServiceType st ON det.ServiceTypeId = st.ServiceTypeId
    WHERE (NOT EXISTS (SELECT 1 FROM #SelectedServiceTypes) 
           OR det.ServiceTypeId IN (SELECT ServiceTypeId FROM #SelectedServiceTypes)
           OR st.ServiceTypeName IN (SELECT ServiceTypeId FROM #SelectedServiceTypes))
    ORDER BY det.JobNo, det.TC;

    -- Cleanup
    DROP TABLE #MasterRecords;
    DROP TABLE #SelectedJobs;
    DROP TABLE #SelectedCustomers;
    DROP TABLE #SelectedShipmentModes;
    DROP TABLE #SelectedServiceTypes;
END
GO
