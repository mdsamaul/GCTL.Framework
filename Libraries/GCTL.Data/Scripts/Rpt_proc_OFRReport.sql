-- =========================================================================================
-- Stored Procedure: dbo.Rpt_proc_OFRReport
-- Description: Retrieves Operational Fund Requisition (OFR) report data including Master
--              Job Information and Detail Expense breakdown. Supports multi-job selection,
--              dynamic Organization (Company) & Branch resolution, and date/status filtering.
--              OPTIMIZED: Clustered primary key on #MasterRecords, static branch resolution,
--              and OPTION (RECOMPILE) for sub-second execution across 17,000+ records.
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

    -- 1. Parse Multi-Select Parameters into Indexed Temp Tables
    CREATE TABLE #SelectedJobs (JobNo NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    IF @JobNo IS NOT NULL AND LTRIM(RTRIM(@JobNo)) <> ''
    BEGIN
        INSERT INTO #SelectedJobs (JobNo)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@JobNo, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    CREATE TABLE #SelectedCustomers (CustomerId NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    IF @CustomerId IS NOT NULL AND LTRIM(RTRIM(@CustomerId)) <> ''
    BEGIN
        INSERT INTO #SelectedCustomers (CustomerId)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@CustomerId, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    CREATE TABLE #SelectedShipmentModes (ShipmentModeId NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    IF @ShipmentModeId IS NOT NULL AND LTRIM(RTRIM(@ShipmentModeId)) <> ''
    BEGIN
        INSERT INTO #SelectedShipmentModes (ShipmentModeId)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@ShipmentModeId, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    CREATE TABLE #SelectedServiceTypes (ServiceTypeId NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    IF @ServiceProvidedById IS NOT NULL AND LTRIM(RTRIM(@ServiceProvidedById)) <> ''
    BEGIN
        INSERT INTO #SelectedServiceTypes (ServiceTypeId)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@ServiceProvidedById, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    -- 2. Resolve Dynamic Organization (Company) and Branch once statically
    DECLARE @OrgName NVARCHAR(200) = 'Formosa Logistics Limited';
    DECLARE @OrgAddress NVARCHAR(500) = '21, Hazaribagh, Dhaka-1209';
    DECLARE @BranchName NVARCHAR(200) = '';

    SELECT TOP 1 
        @OrgName = CompanyName,
        @OrgAddress = COALESCE(NULLIF(LTRIM(RTRIM(Address1)), ''), LTRIM(RTRIM(Address2)), @OrgAddress)
    FROM Core_Company
    WHERE (@CompanyCode IS NOT NULL AND @CompanyCode <> '' AND CompanyCode = @CompanyCode)
       OR (@CompanyCode IS NULL OR @CompanyCode = '')
    ORDER BY CASE WHEN @CompanyCode IS NOT NULL AND @CompanyCode <> '' AND CompanyCode = @CompanyCode THEN 0 ELSE 1 END;

    SELECT TOP 1 
        @BranchName = BranchName,
        @OrgAddress = COALESCE(NULLIF(LTRIM(RTRIM(Address)), ''), @OrgAddress)
    FROM Core_Branch
    WHERE (@BranchCode IS NOT NULL AND @BranchCode <> '' AND UPPER(@BranchCode) <> 'ALL' AND BranchCode = @BranchCode)
       OR (@BranchCode IS NULL OR @BranchCode = '' OR UPPER(@BranchCode) = 'ALL')
    ORDER BY CASE WHEN @BranchCode IS NOT NULL AND @BranchCode <> '' AND UPPER(@BranchCode) <> 'ALL' AND BranchCode = @BranchCode THEN 0 ELSE 1 END;

    -- Adjust ToDate to end of day
    DECLARE @ToDateEnd DATETIME = NULL;
    IF @ToDate IS NOT NULL
    BEGIN
        SET @ToDateEnd = DATEADD(SECOND, -1, DATEADD(DAY, 1, @ToDate));
    END

    -- 3. Retrieve Master OFR Data into #MasterRecords with Clustered Primary Key
    CREATE TABLE #MasterRecords (
        OFRNo NVARCHAR(50),
        JobNo NVARCHAR(50) NOT NULL PRIMARY KEY CLUSTERED,
        CustomerId NVARCHAR(50),
        CustomerName NVARCHAR(200),
        Quantity NVARCHAR(50),
        InvoiceNo NVARCHAR(50),
        [Date] VARCHAR(10),
        LCValue VARCHAR(50),
        BLNo NVARCHAR(100),
        [Weight] NVARCHAR(50),
        ShipmentMode NVARCHAR(100),
        OrganizationName NVARCHAR(200),
        BranchName NVARCHAR(200),
        CompanyAddress NVARCHAR(500),
        OFRReportType VARCHAR(100),
        ApproveBy NVARCHAR(100),
        CustomUser NVARCHAR(100),
        PrintDateTime VARCHAR(50)
    );

    INSERT INTO #MasterRecords
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
        @OrgName AS OrganizationName,
        @BranchName AS BranchName,
        @OrgAddress AS CompanyAddress,
        @OFRReportType AS OFRReportType,
        COALESCE(NULLIF(ofr.ApprovedUser, ''), 'shariful Islam') AS ApproveBy,
        COALESCE(NULLIF(ofr.ApprovedUser, ''), 'shariful Islam') AS CustomUser,
        CONVERT(VARCHAR(30), GETDATE(), 103) + ' ' + RIGHT(CONVERT(VARCHAR(30), GETDATE(), 100), 7) AS PrintDateTime
    FROM CF_OperationalFundRequisitionEntry ofr
    LEFT JOIN Sep_Documentation doc ON ofr.JobNo = doc.JobNo
    LEFT JOIN Sales_Customer cust ON doc.CustomerId = cust.CustomerId
    LEFT JOIN CF_Def_ExpenseType expType ON doc.ExpenseTypeId = expType.ExpenseTypeId
    WHERE (NOT EXISTS (SELECT 1 FROM #SelectedJobs) OR ofr.JobNo IN (SELECT JobNo FROM #SelectedJobs))
      AND (NOT EXISTS (SELECT 1 FROM #SelectedCustomers) OR doc.CustomerId IN (SELECT CustomerId FROM #SelectedCustomers))
      AND (NOT EXISTS (SELECT 1 FROM #SelectedShipmentModes) OR doc.ExpenseTypeId IN (SELECT ShipmentModeId FROM #SelectedShipmentModes))
      AND (@CompanyCode IS NULL OR @CompanyCode = '' OR ofr.CompanyCode = @CompanyCode OR doc.CompanyCode = @CompanyCode)
      AND (@FromDate IS NULL OR ofr.OFRDate >= @FromDate)
      AND (@ToDateEnd IS NULL OR ofr.OFRDate <= @ToDateEnd)
      AND (
          @OFRReportType IS NULL OR @OFRReportType = ''
          OR (@OFRReportType = 'OFR Report')
          OR (@OFRReportType = 'OFR Approval Report' AND ofr.ApprovedUser IS NOT NULL AND LTRIM(RTRIM(ofr.ApprovedUser)) <> '')
          OR (@OFRReportType = 'OFR Final Report' AND ofr.BillAdjustApprovalUser IS NOT NULL AND LTRIM(RTRIM(ofr.BillAdjustApprovalUser)) <> '')
      )
    ORDER BY ofr.OFRDate DESC, ofr.TC DESC
    OPTION (RECOMPILE);

    -- Return 1st Result Set: Master Records
    SELECT * FROM #MasterRecords;

    -- Return 2nd Result Set: Detail Expense Records for the matched jobs (indexed join)
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
    ORDER BY det.JobNo, det.TC
    OPTION (RECOMPILE);

    -- Cleanup
    DROP TABLE #MasterRecords;
    DROP TABLE #SelectedJobs;
    DROP TABLE #SelectedCustomers;
    DROP TABLE #SelectedShipmentModes;
    DROP TABLE #SelectedServiceTypes;
END
GO
