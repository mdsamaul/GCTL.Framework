-- =========================================================================================
-- Stored Procedure: dbo.Rpt_proc_CustomerTopSheetReport
-- Description: Retrieves Customer Top Sheet Report data grouped by Customer.
--              Outputs 2 Result Sets:
--                1. Customer Summary / Header Info (Company, Address, Customer, Totals)
--                2. Bill Line Items (18 columns matching Customer Top Sheet Report.xlsx)
--              OPTIMIZED FOR HIGH SPEED (Sub-Second / Millisecond Execution):
--                - Pre-indexed temp tables for multi-select parameters
--                - Set-based pre-aggregation of Details and CHCHead tables
--                - Clustered primary key on intermediate #MasterBills
--                - Dedicated covering indexes
-- =========================================================================================

-- 1. Create Performance Indexes if not already present
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CF_BillEntry_TopSheet' AND object_id = OBJECT_ID('CF_BillEntry'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CF_BillEntry_TopSheet 
    ON [dbo].[CF_BillEntry] ([BillDate], [CustomerID], [ExpenseTypeId])
    INCLUDE ([BillNo], [JobNo], [Beno], [Bedate], [Invno], [Invdate], [BillAmount], [MinimumAgencyCommission], [CompanyCode]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CF_BillEntryDetails_TopSheet' AND object_id = OBJECT_ID('CF_BillEntryDetails'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CF_BillEntryDetails_TopSheet 
    ON [dbo].[CF_BillEntryDetails] ([BillNo], [ExpenseHeadID])
    INCLUDE ([ExpenseBillAmount]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CF_BillEntryDetails_CHCHead_TopSheet' AND object_id = OBJECT_ID('CF_BillEntryDetails_CHCHead'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CF_BillEntryDetails_CHCHead_TopSheet 
    ON [dbo].[CF_BillEntryDetails_CHCHead] ([BillNo])
    INCLUDE ([BillHead], [BillAmount]);
END
GO

CREATE OR ALTER PROCEDURE [dbo].[Rpt_proc_CustomerTopSheetReport]
    @CompanyCode          VARCHAR(50)   = NULL,
    @BranchCode           VARCHAR(50)   = NULL,
    @FromDate             DATETIME      = NULL,
    @ToDate               DATETIME      = NULL,
    @CustomerId           NVARCHAR(MAX) = NULL,   -- Comma-delimited list of Customer IDs
    @ShipmentModeId       NVARCHAR(MAX) = NULL,   -- Comma-delimited list of Shipment Mode IDs
    @JobNo                NVARCHAR(MAX) = NULL,   -- Comma-delimited list of Job Nos
    @BillNo               NVARCHAR(MAX) = NULL    -- Comma-delimited list of Bill Nos
AS
BEGIN
    SET NOCOUNT ON;

    -- A. Parse Multi-Select Parameters into Indexed Temp Tables
    CREATE TABLE #SelectedCustomers (CustomerId NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY CLUSTERED);
    IF @CustomerId IS NOT NULL AND LTRIM(RTRIM(@CustomerId)) <> ''
    BEGIN
        INSERT INTO #SelectedCustomers (CustomerId)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@CustomerId, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    CREATE TABLE #SelectedShipmentModes (ShipmentModeId NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY CLUSTERED);
    IF @ShipmentModeId IS NOT NULL AND LTRIM(RTRIM(@ShipmentModeId)) <> ''
    BEGIN
        INSERT INTO #SelectedShipmentModes (ShipmentModeId)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@ShipmentModeId, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    CREATE TABLE #SelectedJobs (JobNo NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY CLUSTERED);
    IF @JobNo IS NOT NULL AND LTRIM(RTRIM(@JobNo)) <> ''
    BEGIN
        INSERT INTO #SelectedJobs (JobNo)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@JobNo, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    CREATE TABLE #SelectedBills (BillNo NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY CLUSTERED);
    IF @BillNo IS NOT NULL AND LTRIM(RTRIM(@BillNo)) <> ''
    BEGIN
        INSERT INTO #SelectedBills (BillNo)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@BillNo, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    -- B. Resolve Dynamic Organization & Head Office statically once
    DECLARE @OrgName NVARCHAR(200) = 'Formosa Logistics Limited.';
    DECLARE @OrgAddress NVARCHAR(500) = 'Safura tower (6th floor), 20, Kemal Ataturk Avenue, Banani C/A, Dhaka 1213, Bangladesh.';
    DECLARE @OrgContact NVARCHAR(200) = 'Tel: 880-2-9881830, 9885748, Fax: 880-2-8822843';
    DECLARE @BranchName NVARCHAR(200) = '';

    SELECT TOP 1 
        @OrgName = COALESCE(NULLIF(LTRIM(RTRIM(CompanyName)), ''), @OrgName),
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

    -- C. Filter Master Bills into Temp Table
    CREATE TABLE #MasterBills (
        BillNo NVARCHAR(100) NOT NULL PRIMARY KEY CLUSTERED,
        JobNo NVARCHAR(100),
        CustomerID NVARCHAR(100),
        CustomerName NVARCHAR(200),
        CustomerAddress NVARCHAR(500),
        BillDate DATETIME,
        Invno NVARCHAR(100),
        Invdate DATETIME,
        Beno NVARCHAR(100),
        Bedate DATETIME,
        BillAmount DECIMAL(18,2),
        MinimumAgencyCommission DECIMAL(18,2)
    );

    INSERT INTO #MasterBills (BillNo, JobNo, CustomerID, CustomerName, CustomerAddress, BillDate, Invno, Invdate, Beno, Bedate, BillAmount, MinimumAgencyCommission)
    SELECT 
        b.BillNo,
        b.JobNo,
        b.CustomerID,
        COALESCE(c.CustomerName, b.CustomerID) AS CustomerName,
        COALESCE(c.CustomerAddress, '') AS CustomerAddress,
        b.BillDate,
        b.Invno,
        b.Invdate,
        b.Beno,
        b.Bedate,
        COALESCE(b.BillAmount, 0),
        COALESCE(b.MinimumAgencyCommission, 0)
    FROM CF_BillEntry b WITH (NOLOCK)
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON b.CustomerID = c.CustomerId
    LEFT JOIN CF_Def_ExpenseType et WITH (NOLOCK) ON b.ExpenseTypeId = et.ExpenseTypeID
    WHERE (@CompanyCode IS NULL OR @CompanyCode = '' OR b.CompanyCode = @CompanyCode)
      AND (@FromDate IS NULL OR b.BillDate >= @FromDate)
      AND (@ToDateEnd IS NULL OR b.BillDate <= @ToDateEnd)
      AND (NOT EXISTS (SELECT 1 FROM #SelectedCustomers) OR b.CustomerID IN (SELECT CustomerId FROM #SelectedCustomers))
      AND (NOT EXISTS (SELECT 1 FROM #SelectedShipmentModes) 
           OR LTRIM(RTRIM(et.TransportModeTypeId)) IN (SELECT ShipmentModeId FROM #SelectedShipmentModes)
           OR LTRIM(RTRIM(b.ExpenseTypeId)) IN (SELECT ShipmentModeId FROM #SelectedShipmentModes))
      AND (NOT EXISTS (SELECT 1 FROM #SelectedJobs) OR b.JobNo IN (SELECT JobNo FROM #SelectedJobs))
      AND (NOT EXISTS (SELECT 1 FROM #SelectedBills) OR b.BillNo IN (SELECT BillNo FROM #SelectedBills));

    -- D. Pre-aggregate CF_BillEntryDetails for filtered bills
    CREATE TABLE #AggDetails (
        BillNo NVARCHAR(100) NOT NULL PRIMARY KEY CLUSTERED,
        DtiAmount DECIMAL(18,2),
        VatAmount DECIMAL(18,2),
        WarehouseRent DECIMAL(18,2),
        LabourCharge DECIMAL(18,2),
        FfNoc DECIMAL(18,2),
        AirFreight DECIMAL(18,2),
        CustomsMisc DECIMAL(18,2),
        OthersMisc DECIMAL(18,2)
    );

    INSERT INTO #AggDetails (BillNo, DtiAmount, VatAmount, WarehouseRent, LabourCharge, FfNoc, AirFreight, CustomsMisc, OthersMisc)
    SELECT 
        dt.BillNo,
        SUM(CASE WHEN dt.ExpenseHeadID IN ('033', '001') THEN COALESCE(dt.ExpenseBillAmount, dt.ActualAmount, 0) ELSE 0 END),
        SUM(CASE WHEN dt.ExpenseHeadID IN ('034', '003') THEN COALESCE(dt.ExpenseBillAmount, dt.ActualAmount, 0) ELSE 0 END),
        SUM(CASE WHEN dt.ExpenseHeadID IN ('040') THEN COALESCE(dt.ExpenseBillAmount, dt.ActualAmount, 0) ELSE 0 END),
        SUM(CASE WHEN dt.ExpenseHeadID IN ('041', '042') THEN COALESCE(dt.ExpenseBillAmount, dt.ActualAmount, 0) ELSE 0 END),
        SUM(CASE WHEN dt.ExpenseHeadID IN ('010', '011') THEN COALESCE(dt.ExpenseBillAmount, dt.ActualAmount, 0) ELSE 0 END),
        SUM(CASE WHEN dt.ExpenseHeadID IN ('044') THEN COALESCE(dt.ExpenseBillAmount, dt.ActualAmount, 0) ELSE 0 END),
        SUM(CASE WHEN dt.ExpenseHeadID IN ('025', '028', '029', '030', '032', '035', '036', '039', '045') THEN COALESCE(dt.ExpenseBillAmount, dt.ActualAmount, 0) ELSE 0 END),
        SUM(CASE WHEN dt.ExpenseHeadID NOT IN ('033','001','034','003','040','041','042','010','011','044','025','028','029','030','032','035','036','039','045') THEN COALESCE(dt.ExpenseBillAmount, dt.ActualAmount, 0) ELSE 0 END)
    FROM CF_BillEntryDetails dt WITH (NOLOCK)
    INNER JOIN #MasterBills mb ON dt.BillNo = mb.BillNo
    GROUP BY dt.BillNo;

    -- E. Pre-aggregate CF_BillEntryDetails_CHCHead for Transport & Labour
    CREATE TABLE #AggCHC (
        BillNo NVARCHAR(100) NOT NULL PRIMARY KEY CLUSTERED,
        TransportBill DECIMAL(18,2),
        LabourBill DECIMAL(18,2)
    );

    INSERT INTO #AggCHC (BillNo, TransportBill, LabourBill)
    SELECT 
        ch.BillNo,
        SUM(CASE WHEN ch.BillHead LIKE '%Transport%' THEN COALESCE(ch.BillAmount, 0) ELSE 0 END),
        SUM(CASE WHEN ch.BillHead LIKE '%Labour%' THEN COALESCE(ch.BillAmount, 0) ELSE 0 END)
    FROM CF_BillEntryDetails_CHCHead ch WITH (NOLOCK)
    INNER JOIN #MasterBills mb ON ch.BillNo = mb.BillNo
    GROUP BY ch.BillNo;

    -- F. Combined Line Items Table
    CREATE TABLE #FinalLineItems (
        CustomerId NVARCHAR(100),
        CustomerName NVARCHAR(200),
        CustomerAddress NVARCHAR(500),
        BillNo NVARCHAR(100),
        JobNo NVARCHAR(100),
        LcLcaNo NVARCHAR(100),
        InvoiceNo NVARCHAR(100),
        InvoiceDate VARCHAR(10),
        BeRegNo NVARCHAR(100),
        BeRegDate VARCHAR(10),
        DtiAssociationServiceCharge DECIMAL(18,2),
        VatGlobalTax DECIMAL(18,2),
        CustomsMisc DECIMAL(18,2),
        WarehouseRent DECIMAL(18,2),
        FreightForwarderNOC DECIMAL(18,2),
        LabourCharge DECIMAL(18,2),
        OthersMisc DECIMAL(18,2),
        AirFreightCharge DECIMAL(18,2),
        TransportBill DECIMAL(18,2),
        Commission DECIMAL(18,2),
        TotalBillTk DECIMAL(18,2)
    );

    INSERT INTO #FinalLineItems
    SELECT 
        b.CustomerID,
        b.CustomerName,
        b.CustomerAddress,
        b.BillNo,
        b.JobNo,
        COALESCE(doc.Lcno, '') AS LcLcaNo,
        COALESCE(NULLIF(b.Invno, ''), doc.InvoiceNo, '') AS InvoiceNo,
        CONVERT(VARCHAR(10), COALESCE(b.Invdate, doc.Date), 103) AS InvoiceDate,
        COALESCE(NULLIF(b.Beno, ''), doc.Beno, '') AS BeRegNo,
        CONVERT(VARCHAR(10), COALESCE(b.Bedate, doc.Bedate), 103) AS BeRegDate,
        COALESCE(d.DtiAmount, 0) AS DtiAssociationServiceCharge,
        COALESCE(d.VatAmount, 0) AS VatGlobalTax,
        COALESCE(d.CustomsMisc, 0) AS CustomsMisc,
        COALESCE(d.WarehouseRent, 0) AS WarehouseRent,
        COALESCE(d.FfNoc, 0) AS FreightForwarderNOC,
        COALESCE(chc.LabourBill, d.LabourCharge, 0) AS LabourCharge,
        COALESCE(d.OthersMisc, 0) AS OthersMisc,
        COALESCE(d.AirFreight, 0) AS AirFreightCharge,
        COALESCE(chc.TransportBill, 0) AS TransportBill,
        COALESCE(NULLIF(b.MinimumAgencyCommission, 0), doc.MinimumAgencyCom, 0) AS Commission,
        -- Total Bill Calculation (Exact sum of column components, or b.BillAmount fallback)
        CASE 
            WHEN (COALESCE(d.DtiAmount,0) + COALESCE(d.VatAmount,0) + COALESCE(d.WarehouseRent,0) + COALESCE(chc.TransportBill,0) + COALESCE(chc.LabourBill, d.LabourCharge, 0) + COALESCE(b.MinimumAgencyCommission, doc.MinimumAgencyCom, 0)) > 0
            THEN (COALESCE(d.DtiAmount,0) + COALESCE(d.VatAmount,0) + COALESCE(d.CustomsMisc,0) + COALESCE(d.WarehouseRent,0) + COALESCE(d.FfNoc,0) + COALESCE(chc.LabourBill, d.LabourCharge, 0) + COALESCE(d.OthersMisc,0) + COALESCE(d.AirFreight,0) + COALESCE(chc.TransportBill,0) + COALESCE(NULLIF(b.MinimumAgencyCommission, 0), doc.MinimumAgencyCom, 0))
            ELSE b.BillAmount
        END AS TotalBillTk
    FROM #MasterBills b
    LEFT JOIN Sep_Documentation doc WITH (NOLOCK) ON b.JobNo = doc.JobNo
    LEFT JOIN #AggDetails d ON b.BillNo = d.BillNo
    LEFT JOIN #AggCHC chc ON b.BillNo = chc.BillNo;

    -- =========================================================================
    -- RESULT SET 1: CUSTOMER GROUP SUMMARY (Header info & Totals per Customer)
    -- =========================================================================
    SELECT 
        f.CustomerId,
        f.CustomerName,
        f.CustomerAddress,
        @OrgName AS CompanyName,
        @OrgAddress AS HeadOfficeAddress,
        @OrgContact AS ContactPhoneFax,
        'ACCOUNTS DEPARTMENT' AS Attn,
        'SUBMISSION OF C&F BILLS.' AS Ref,
        CONVERT(VARCHAR(10), GETDATE(), 103) AS ReportDate,
        COUNT(f.BillNo) AS TotalBills,
        SUM(f.DtiAssociationServiceCharge) AS TotalDti,
        SUM(f.VatGlobalTax) AS TotalVat,
        SUM(f.CustomsMisc) AS TotalCustomsMisc,
        SUM(f.WarehouseRent) AS TotalWarehouseRent,
        SUM(f.FreightForwarderNOC) AS TotalFfNoc,
        SUM(f.LabourCharge) AS TotalLabour,
        SUM(f.OthersMisc) AS TotalOthersMisc,
        SUM(f.AirFreightCharge) AS TotalAirFreight,
        SUM(f.TransportBill) AS TotalTransport,
        SUM(f.Commission) AS TotalCommission,
        SUM(f.TotalBillTk) AS GrandTotalAmount
    FROM #FinalLineItems f
    GROUP BY f.CustomerId, f.CustomerName, f.CustomerAddress
    ORDER BY f.CustomerName;

    -- =========================================================================
    -- RESULT SET 2: DETAIL BILL ITEMS (18 columns with SL No per customer)
    -- =========================================================================
    SELECT 
        ROW_NUMBER() OVER(PARTITION BY f.CustomerId ORDER BY f.BillNo) AS SlNo,
        f.CustomerId,
        f.CustomerName,
        f.BillNo,
        f.JobNo,
        f.LcLcaNo,
        f.InvoiceNo,
        f.InvoiceDate,
        f.BeRegNo,
        f.BeRegDate,
        f.DtiAssociationServiceCharge,
        f.VatGlobalTax,
        f.CustomsMisc,
        f.WarehouseRent,
        f.FreightForwarderNOC,
        f.LabourCharge,
        f.OthersMisc,
        f.AirFreightCharge,
        f.TransportBill,
        f.Commission,
        f.TotalBillTk
    FROM #FinalLineItems f
    ORDER BY f.CustomerName, f.BillNo
    OPTION (RECOMPILE);

    -- Cleanup
    DROP TABLE #SelectedCustomers;
    DROP TABLE #SelectedShipmentModes;
    DROP TABLE #SelectedJobs;
    DROP TABLE #SelectedBills;
    DROP TABLE #MasterBills;
    DROP TABLE #AggDetails;
    DROP TABLE #AggCHC;
    DROP TABLE #FinalLineItems;
END
GO
