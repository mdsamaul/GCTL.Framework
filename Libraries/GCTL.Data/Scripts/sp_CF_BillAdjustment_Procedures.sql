-- =========================================================================
-- Script Name: sp_CF_BillAdjustment_Procedures.sql
-- Module: C&F / Freight Forwarding - Bill Adjustment Entry
-- Objects: 
--   1. Indexes on FinalBillInfo_ForBillAdjust, CF_BillEntryDetails_CHCHead_ForBillAdjust, CF_BillAdjustDetails
--   2. User-Defined Table Type: UT_CF_BillAdjustDetails
--   3. Stored Procedure: Proc_LoadApprovedBillListForAdjust
--   4. Stored Procedure: proc_CF_BillInfoF_BillAdjust
--   5. Stored Procedure: sp_Save_CF_BillAdjustmentEntry
-- =========================================================================

USE [GCTL_ERP_DB_Copy];
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- -------------------------------------------------------------------------
-- 1. Performance Indexes
-- -------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FinalBillInfo_BillDate_Cover' AND object_id = OBJECT_ID('dbo.FinalBillInfo_ForBillAdjust'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_FinalBillInfo_BillDate_Cover
    ON dbo.FinalBillInfo_ForBillAdjust (BillDate DESC)
    INCLUDE (BillNo, JobNo, CustomerId, CustomerName, Beno, DeliveryDate, TotalAmount, AdvanceAmount);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CF_BillEntryDetails_CHCHead_BillNo' AND object_id = OBJECT_ID('dbo.CF_BillEntryDetails_CHCHead_ForBillAdjust'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CF_BillEntryDetails_CHCHead_BillNo
    ON dbo.CF_BillEntryDetails_CHCHead_ForBillAdjust (BillNo)
    INCLUDE (Chcid, BillHead, ChcheadDescription, BillAmount, IsReceiptable);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CF_BillAdjustDetails_AdjNo' AND object_id = OBJECT_ID('dbo.CF_BillAdjustDetails'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CF_BillAdjustDetails_AdjNo
    ON dbo.CF_BillAdjustDetails (AdjustmentNo)
    INCLUDE (Chcid, BillAmount, EmployeeId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CF_BillEntry_BillDate_Cover' AND object_id = OBJECT_ID('dbo.CF_BillEntry'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CF_BillEntry_BillDate_Cover
    ON dbo.CF_BillEntry (BillDate DESC)
    INCLUDE (BillNo, JobNo, CustomerID, BENo, DeliveryDate, BillAmount, CompanyCode);
END
GO

-- -------------------------------------------------------------------------
-- 2. User-Defined Table Type for Details
-- -------------------------------------------------------------------------
IF TYPE_ID('dbo.UT_CF_BillAdjustDetails') IS NULL
BEGIN
    CREATE TYPE dbo.UT_CF_BillAdjustDetails AS TABLE
    (
        Chcid       NVARCHAR(50)    NOT NULL,
        BillAmount  NUMERIC(18, 2)  NOT NULL DEFAULT(0),
        EmployeeId  NVARCHAR(50)    NULL
    );
END
GO

-- -------------------------------------------------------------------------
-- 3. Stored Procedure: Proc_LoadApprovedBillListForAdjust
-- -------------------------------------------------------------------------
CREATE OR ALTER PROC dbo.Proc_LoadApprovedBillListForAdjust
    @FromDate    DATETIME      = NULL,
    @ToDate      DATETIME      = NULL,
    @SearchTerm  NVARCHAR(100) = NULL,
    @CompanyCode NVARCHAR(50)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDateOnly DATETIME = CASE WHEN @FromDate IS NOT NULL THEN CAST(@FromDate AS DATE) ELSE NULL END;
    DECLARE @ToDateExcl DATETIME = CASE WHEN @ToDate IS NOT NULL THEN DATEADD(DAY, 1, CAST(@ToDate AS DATE)) ELSE NULL END;
    SET @SearchTerm = LTRIM(RTRIM(ISNULL(@SearchTerm, '')));
    SET @CompanyCode = LTRIM(RTRIM(ISNULL(@CompanyCode, '')));

    -- 1. If FinalBillInfo_ForBillAdjust has rows, query it
    IF EXISTS (SELECT 1 FROM dbo.FinalBillInfo_ForBillAdjust WITH (NOLOCK))
    BEGIN
        SELECT DISTINCT
            fb.BillNo,
            COALESCE(TRY_CAST(fb.BillDate AS DATETIME), TRY_CONVERT(DATETIME, fb.BillDate, 103), TRY_CONVERT(DATETIME, fb.BillDate, 105), TRY_CONVERT(DATETIME, fb.BillDate, 120), TRY_CONVERT(DATETIME, fb.BillDate, 106)) AS BillDate,
            ISNULL(fb.JobNo, '')                            AS JobNo,
            ISNULL(fb.CustomerId, '')                       AS CustomerId,
            ISNULL(fb.CustomerName, '')                     AS CustomerName,
            ISNULL(fb.Beno, '')                             AS BeNo,
            COALESCE(TRY_CAST(fb.DeliveryDate AS DATETIME), TRY_CONVERT(DATETIME, fb.DeliveryDate, 103), TRY_CONVERT(DATETIME, fb.DeliveryDate, 105), TRY_CONVERT(DATETIME, fb.DeliveryDate, 120), TRY_CONVERT(DATETIME, fb.DeliveryDate, 106)) AS DeliveryDate,
            ISNULL(fb.TotalAmount, 0)                       AS TotalAmount,
            ISNULL(fb.AdvanceAmount, 0)                     AS AdvanceAmount,
            (ISNULL(fb.TotalAmount, 0) - ISNULL(fb.AdvanceAmount, 0)) AS NetPayable,
            ISNULL(fb.ExpenseType, 'Sea Export (CTG)')      AS ServiceType,
            ISNULL(v.VoucherNo, '')                         AS VoucherNo,
            ISNULL(adj.AdjustmentType, '')                  AS AdjustmentType,
            ISNULL(adj.AdjustmentNo, '')                    AS AdjustmentNo,
            adj.AdjustmentDate                              AS AdjustmentDate
        FROM dbo.FinalBillInfo_ForBillAdjust fb WITH (NOLOCK)
        OUTER APPLY (
            SELECT TOP 1 v.VoucherNo 
            FROM dbo.Acc_VoucherEntry v WITH (NOLOCK) 
            WHERE v.InvoiceNo = fb.BillNo 
            ORDER BY v.autoId DESC
        ) v
        OUTER APPLY (
            SELECT TOP 1 adj.AdjustmentNo, adj.AdjustmentType, adj.AdjustmentDate 
            FROM dbo.CF_BillAdjustmentEntry adj WITH (NOLOCK) 
            WHERE adj.BillNo = fb.BillNo 
            ORDER BY adj.TC DESC
        ) adj
        WHERE (
                @FromDateOnly IS NULL
                OR COALESCE(TRY_CAST(fb.BillDate AS DATE), TRY_CONVERT(DATE, fb.BillDate, 103), TRY_CONVERT(DATE, fb.BillDate, 105), TRY_CONVERT(DATE, fb.BillDate, 120), TRY_CONVERT(DATE, fb.BillDate, 106)) >= @FromDateOnly
              )
          AND (
                @ToDateExcl IS NULL
                OR COALESCE(TRY_CAST(fb.BillDate AS DATE), TRY_CONVERT(DATE, fb.BillDate, 103), TRY_CONVERT(DATE, fb.BillDate, 105), TRY_CONVERT(DATE, fb.BillDate, 120), TRY_CONVERT(DATE, fb.BillDate, 106)) < @ToDateExcl
              )
          AND (
                @SearchTerm = '' 
                OR fb.BillNo LIKE '%' + @SearchTerm + '%'
                OR fb.JobNo LIKE '%' + @SearchTerm + '%'
                OR fb.CustomerName LIKE '%' + @SearchTerm + '%'
                OR fb.Beno LIKE '%' + @SearchTerm + '%'
              )
          AND (
                @CompanyCode = '' 
                OR fb.CompanyName = @CompanyCode 
                OR fb.CompanyName LIKE '%' + @CompanyCode + '%'
              )
        ORDER BY BillDate DESC, fb.BillNo DESC
        OPTION (RECOMPILE);
    END
    -- 2. Otherwise query live CF_BillEntry + Sales_Customer
    ELSE
    BEGIN
        SELECT
            b.BillNo,
            b.BillDate                                      AS BillDate,
            ISNULL(b.JobNo, '')                             AS JobNo,
            ISNULL(b.CustomerID, '')                        AS CustomerId,
            ISNULL(c.CustomerName, '')                      AS CustomerName,
            ISNULL(b.BENo, '')                              AS BeNo,
            COALESCE(b.DeliveryDate, s.DateOfClearence, b.BEDate, b.DocRecvDate, b.BillDate) AS DeliveryDate,
            ISNULL(b.BillAmount, 0)                         AS TotalAmount,
            0.00                                            AS AdvanceAmount,
            ISNULL(b.BillAmount, 0)                         AS NetPayable,
            COALESCE(et.ExpenseType, 'Sea Export (CTG)')    AS ServiceType,
            ISNULL(v.VoucherNo, '')                         AS VoucherNo,
            ISNULL(adj.AdjustmentType, '')                  AS AdjustmentType,
            ISNULL(adj.AdjustmentNo, '')                    AS AdjustmentNo,
            adj.AdjustmentDate                              AS AdjustmentDate
        FROM dbo.CF_BillEntry b WITH (NOLOCK)
        LEFT JOIN dbo.Sales_Customer c WITH (NOLOCK) ON b.CustomerID = c.CustomerID
        LEFT JOIN dbo.CF_Def_ExpenseType et WITH (NOLOCK) ON b.ExpenseTypeID = et.ExpenseTypeID
        LEFT JOIN dbo.Sep_Documentation s WITH (NOLOCK) ON b.JobNo = s.JobNo
        OUTER APPLY (
            SELECT TOP 1 v.VoucherNo 
            FROM dbo.Acc_VoucherEntry v WITH (NOLOCK) 
            WHERE v.InvoiceNo = b.BillNo 
            ORDER BY v.autoId DESC
        ) v
        OUTER APPLY (
            SELECT TOP 1 adj.AdjustmentNo, adj.AdjustmentType, adj.AdjustmentDate 
            FROM dbo.CF_BillAdjustmentEntry adj WITH (NOLOCK) 
            WHERE adj.BillNo = b.BillNo 
            ORDER BY adj.TC DESC
        ) adj
        WHERE (
                @FromDateOnly IS NULL
                OR b.BillDate >= @FromDateOnly
              )
          AND (
                @ToDateExcl IS NULL
                OR b.BillDate < @ToDateExcl
              )
          AND (
                @SearchTerm = '' 
                OR b.BillNo LIKE '%' + @SearchTerm + '%'
                OR b.JobNo LIKE '%' + @SearchTerm + '%'
                OR c.CustomerName LIKE '%' + @SearchTerm + '%'
                OR b.BENo LIKE '%' + @SearchTerm + '%'
              )
          AND (
                @CompanyCode = '' 
                OR b.CompanyCode = @CompanyCode
              )
        ORDER BY b.BillDate DESC, b.BillNo DESC
        OPTION (RECOMPILE);
    END
END
GO

-- -------------------------------------------------------------------------
-- 4. Stored Procedure: proc_CF_BillInfoF_BillAdjust
-- -------------------------------------------------------------------------
CREATE OR ALTER PROC dbo.proc_CF_BillInfoF_BillAdjust
    @BillNo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SET @BillNo = LTRIM(RTRIM(ISNULL(@BillNo, '')));

    IF @BillNo = ''
    BEGIN
        RAISERROR('Bill No is required.', 16, 1);
        RETURN;
    END

    -- Result Set 1: Header Info
    IF EXISTS (SELECT 1 FROM dbo.FinalBillInfo_ForBillAdjust WITH (NOLOCK) WHERE BillNo = @BillNo)
    BEGIN
        SELECT TOP 1
            fb.BillNo,
            'SLSL'                                          AS BillNoPrefix,
            TRY_CAST(fb.BillDate AS DATETIME)               AS BillDate,
            ISNULL(fb.JobNo, '')                            AS JobNo,
            TRY_CAST(fb.BillDate AS DATETIME)               AS JobDate,
            ISNULL(fb.BranchName, 'Main Branch')            AS Branch,
            ISNULL(fb.ExpenseType, 'C&F')                   AS ServiceType,
            ISNULL(fb.CustomerId, '')                       AS CustomerId,
            ISNULL(fb.CustomerName, '')                     AS CustomerName,
            ISNULL(fb.CustomerAddress, '')                  AS CustomerAddress,
            ISNULL(fb.Address, '')                          AS CustomerDeliveryAddress,
            ISNULL(fb.CustomerName, '')                     AS SupplierName,
            'Chittagong Port'                               AS PortName,
            TRY_CAST(fb.DocRecvDate AS DATETIME)            AS DocRecvDate,
            ISNULL(fb.INVNo, '')                            AS LcNo,
            0.00                                            AS LcValue,
            'USD'                                           AS LcCurrency,
            0.00                                            AS BdtRate,
            1                                               AS NumberOfTransport,
            0                                               AS NumberOfDemerageDay,
            ISNULL(fb.IPNo, '')                             AS IpNo,
            TRY_CAST(fb.IPDate AS DATETIME)                 AS IpDate,
            ISNULL(fb.UndertakingNo, '')                    AS UndertakingNo,
            TRY_CAST(fb.ArrivalDate AS DATETIME)            AS ArrivalDate,
            ISNULL(fb.INVNo, '')                            AS InvNo,
            TRY_CAST(fb.INVDate AS DATETIME)                AS InvDate,
            ISNULL(fb.Blno, '')                             AS BlNo,
            TRY_CAST(fb.BLDate AS DATETIME)                 AS BlDate,
            ISNULL(fb.Beno, '')                             AS BeNo,
            TRY_CAST(fb.Bedate AS DATETIME)                 AS BeDate,
            ISNULL(fb.EFRNo, '')                            AS EfrNo,
            TRY_CAST(fb.EFRDate AS DATETIME)                AS EfrDate,
            ISNULL(fb.ROTNo, '')                            AS RotNo,
            ISNULL(fb.OFRNo, '')                            AS PoNo,
            ISNULL(fb.ContainerNo, '')                      AS ContainerNo,
            ISNULL(fb.MaterialDescription, '')              AS MaterialDescription,
            ISNULL(fb.Quantity, 0)                          AS Quantity,
            'PCS'                                           AS QUnitType,
            ISNULL(fb.docWeight, 0)                         AS DocWeight,
            'KG'                                            AS WUnitType,
            ISNULL(fb.JRSNo, '')                            AS Fwdr,
            ISNULL(fb.ROTNo, '')                            AS VesselNo,
            TRY_CAST(fb.ArrivalDate AS DATETIME)            AS EtaDate,
            TRY_CAST(fb.ArrivalDate AS DATETIME)            AS AtaDate,
            TRY_CAST(fb.ArrivalDate AS DATETIME)            AS EtbDate,
            TRY_CAST(fb.ArrivalDate AS DATETIME)            AS AtbDate,
            TRY_CAST(fb.DeliveryDate AS DATETIME)           AS UnStuffingDate,
            TRY_CAST(fb.DeliveryDate AS DATETIME)           AS DateOfClearance,
            ISNULL(fb.JRSNo, '')                            AS ConsoleNo,
            ISNULL(fb.Destination, '')                      AS Destination,
            TRY_CAST(fb.DeliveryDate AS DATETIME)           AS DeliveryDate,
            ISNULL(fb.TotalAmount, 0)                       AS GrossAmount,
            ISNULL(fb.AdvanceAmount, 0)                     AS AdvanceAmount,
            (ISNULL(fb.TotalAmount, 0) - ISNULL(fb.AdvanceAmount, 0)) AS NetAmount,
            ISNULL(fb.TotalCfbillAmount, 0)                 AS TotalCfBillAmount,
            ISNULL(fb.TotalTransportBillAmount, 0)          AS TotalTransportBillAmount,
            ISNULL(fb.ITC, 0)                               AS Itc,
            ISNULL(fb.CV, 0)                                AS Cv,
            0.00                                            AS VatCurrAccTransport,
            0.00                                            AS VatCurrAccCf,
            0.00                                            AS VatCurrAccTransportAdjust,
            0.00                                            AS VatCurrAccCfAdjust,
            0.00                                            AS MinimumAgencyCommission,
            0.00                                            AS OnInvoiceValue,
            0.00                                            AS AgencyCommissionAmount,
            0.00                                            AS AssessableRate,
            0.00                                            AS AssessableValue,
            0.00                                            AS InvoiceValue,
            'USD'                                           AS InvoiceCurrency,
            ISNULL(fb.Terms, '')                            AS Terms,
            COALESCE(NULLIF(adj.Remarks, ''), fb.Remarks, '') AS Remarks,
            COALESCE(NULLIF(adj.NotesReasonForCredit, ''), NULLIF(adj.Remarks, ''), fb.Remarks, '') AS NotesReasonForCredit,
            ISNULL(adj.AdjustmentNo, '')                    AS AdjustmentNo,
            ISNULL(adj.AdjustmentType, 'Credit Note')       AS AdjustmentType,
            adj.AdjustmentDate                              AS AdjustmentDate,
            ISNULL(v.VoucherNo, '')                         AS VoucherNo,
            COALESCE(TRY_CAST(fb.ProcessDate AS DATETIME), GETDATE()) AS CreationDate,
            GETDATE()                                       AS LastUpdateDate
        FROM dbo.FinalBillInfo_ForBillAdjust fb WITH (NOLOCK)
        OUTER APPLY (
            SELECT TOP 1 v.VoucherNo 
            FROM dbo.Acc_VoucherEntry v WITH (NOLOCK) 
            WHERE v.InvoiceNo = fb.BillNo 
            ORDER BY v.autoId DESC
        ) v
        OUTER APPLY (
            SELECT TOP 1 adj.AdjustmentNo, adj.AdjustmentType, adj.AdjustmentDate, adj.Remarks, adj.NotesReasonForCredit
            FROM dbo.CF_BillAdjustmentEntry adj WITH (NOLOCK) 
            WHERE adj.BillNo = fb.BillNo 
            ORDER BY adj.TC DESC
        ) adj
        WHERE fb.BillNo = @BillNo;
    END
    ELSE
    BEGIN
        SELECT TOP 1
            b.BillNo,
            ISNULL(b.PrifixId, 'SLSL')                      AS BillNoPrefix,
            b.BillDate                                      AS BillDate,
            ISNULL(b.JobNo, '')                             AS JobNo,
            b.BillDate                                      AS JobDate,
            COALESCE(br.BranchName, comp.CompanyName, 'Head Office (Dhaka)') AS Branch,
            COALESCE(et.ExpenseType, 'Sea Export (CTG)')    AS ServiceType,
            ISNULL(b.CustomerID, '')                        AS CustomerId,
            ISNULL(c.CustomerName, '')                      AS CustomerName,
            ISNULL(c.CustomerAddress, b.Address)            AS CustomerAddress,
            ISNULL(b.Address, '')                           AS CustomerDeliveryAddress,
            ISNULL(c.CustomerName, '')                      AS SupplierName,
            'Chittagong Port'                               AS PortName,
            b.DocRecvDate                                   AS DocRecvDate,
            ISNULL(b.INVNo, '')                             AS LcNo,
            ISNULL(b.LCValue, 0)                            AS LcValue,
            ISNULL(b.LCurrencyID, 'USD')                    AS LcCurrency,
            ISNULL(b.AssessableRate, 0)                     AS BdtRate,
            ISNULL(b.NumberOfTransport, 1)                  AS NumberOfTransport,
            ISNULL(b.NumberOfDemerageDay, 0)                AS NumberOfDemerageDay,
            ISNULL(b.IPNo, '')                              AS IpNo,
            b.IPDate                                        AS IpDate,
            ISNULL(b.UndertakingNo, '')                     AS UndertakingNo,
            b.ArrivalDate                                   AS ArrivalDate,
            ISNULL(b.INVNo, '')                             AS InvNo,
            b.INVDate                                       AS InvDate,
            ISNULL(b.BLNo, '')                              AS BlNo,
            b.BLDate                                        AS BlDate,
            ISNULL(b.BENo, '')                              AS BeNo,
            b.BEDate                                        AS BeDate,
            ISNULL(b.EFRNo, '')                             AS EfrNo,
            b.EFRDate                                       AS EfrDate,
            ISNULL(b.ROTNo, '')                             AS RotNo,
            ISNULL(b.OFRNo, '')                             AS PoNo,
            ISNULL(b.ContainerNo, '')                       AS ContainerNo,
            ISNULL(b.MaterialDescription, '')               AS MaterialDescription,
            ISNULL(b.Quantity, 0)                           AS Quantity,
            ISNULL(qu.ShortName, 'PCS')                     AS QUnitType,
            ISNULL(b.DocWeight, 0)                          AS DocWeight,
            ISNULL(wu.ShortName, 'KG')                      AS WUnitType,
            ISNULL(b.JRSNo, '')                             AS Fwdr,
            ISNULL(b.ROTNo, '')                             AS VesselNo,
            b.ArrivalDate                                   AS EtaDate,
            b.ArrivalDate                                   AS AtaDate,
            b.ArrivalDate                                   AS EtbDate,
            b.ArrivalDate                                   AS AtbDate,
            COALESCE(b.DeliveryDate, s.DateOfClearence, b.BEDate, b.DocRecvDate, b.BillDate) AS UnStuffingDate,
            COALESCE(b.DeliveryDate, s.DateOfClearence, b.BEDate, b.DocRecvDate, b.BillDate) AS DateOfClearance,
            ISNULL(b.JRSNo, '')                             AS ConsoleNo,
            ISNULL(b.Destination, '')                       AS Destination,
            COALESCE(b.DeliveryDate, s.DateOfClearence, b.BEDate, b.DocRecvDate, b.BillDate) AS DeliveryDate,
            ISNULL(b.BillAmount, 0)                         AS GrossAmount,
            0.00                                            AS AdvanceAmount,
            ISNULL(b.BillAmount, 0)                         AS NetAmount,
            ISNULL(b.BillAmount, 0)                         AS TotalCfBillAmount,
            0.00                                            AS TotalTransportBillAmount,
            0.00                                            AS Itc,
            0.00                                            AS Cv,
            0.00                                            AS VatCurrAccTransport,
            0.00                                            AS VatCurrAccCf,
            0.00                                            AS VatCurrAccTransportAdjust,
            0.00                                            AS VatCurrAccCfAdjust,
            ISNULL(b.MinimumAgencyCommission, 0)            AS MinimumAgencyCommission,
            ISNULL(b.OnInvoiceValue, 0)                     AS OnInvoiceValue,
            (ISNULL(b.InvoiceValue, 0) * ISNULL(b.OnInvoiceValue, 0) / 100.0) AS AgencyCommissionAmount,
            ISNULL(b.AssessableRate, 0)                     AS AssessableRate,
            ISNULL(b.AssessableValue, 0)                    AS AssessableValue,
            ISNULL(b.InvoiceValue, 0)                       AS InvoiceValue,
            ISNULL(b.InvoiceCurrencyID, 'USD')              AS InvoiceCurrency,
            ISNULL(b.Terms, '')                             AS Terms,
            COALESCE(NULLIF(adj.Remarks, ''), b.Remarks, '') AS Remarks,
            COALESCE(NULLIF(adj.NotesReasonForCredit, ''), NULLIF(adj.Remarks, ''), b.Remarks, '') AS NotesReasonForCredit,
            ISNULL(adj.AdjustmentNo, '')                    AS AdjustmentNo,
            ISNULL(adj.AdjustmentType, 'Credit Note')       AS AdjustmentType,
            adj.AdjustmentDate                              AS AdjustmentDate,
            ISNULL(v.VoucherNo, '')                         AS VoucherNo,
            b.LDate                                         AS CreationDate,
            b.ModifyDate                                    AS LastUpdateDate
        FROM dbo.CF_BillEntry b WITH (NOLOCK)
        LEFT JOIN dbo.Sales_Customer c WITH (NOLOCK) ON b.CustomerID = c.CustomerID
        LEFT JOIN dbo.Core_Branch br WITH (NOLOCK) ON b.CompanyCode = br.BranchCode
        LEFT JOIN dbo.Core_Company comp WITH (NOLOCK) ON b.CompanyCode = comp.CompanyCode
        LEFT JOIN dbo.CF_Def_ExpenseType et WITH (NOLOCK) ON b.ExpenseTypeID = et.ExpenseTypeID
        LEFT JOIN dbo.Sep_Documentation s WITH (NOLOCK) ON b.JobNo = s.JobNo
        LEFT JOIN dbo.RMG_Prod_Def_UnitType qu WITH (NOLOCK) ON b.QUnitTypeID = qu.UnitTypID
        LEFT JOIN dbo.RMG_Prod_Def_UnitType wu WITH (NOLOCK) ON b.WUnitTypeID = wu.UnitTypID
        OUTER APPLY (
            SELECT TOP 1 v.VoucherNo 
            FROM dbo.Acc_VoucherEntry v WITH (NOLOCK) 
            WHERE v.InvoiceNo = b.BillNo 
            ORDER BY v.autoId DESC
        ) v
        OUTER APPLY (
            SELECT TOP 1 adj.AdjustmentNo, adj.AdjustmentType, adj.AdjustmentDate, adj.Remarks, adj.NotesReasonForCredit
            FROM dbo.CF_BillAdjustmentEntry adj WITH (NOLOCK) 
            WHERE adj.BillNo = b.BillNo 
            ORDER BY adj.TC DESC
        ) adj
        WHERE b.BillNo = @BillNo;
    END

    -- Result Set 2: Grid 1 - Custom Handling Charges
    IF EXISTS (
        SELECT 1 
        FROM dbo.CF_BillAdjustDetails ad WITH (NOLOCK)
        JOIN dbo.CF_BillAdjustmentEntry ae WITH (NOLOCK) ON ad.AdjustmentNo = ae.AdjustmentNo
        WHERE ae.BillNo = @BillNo
    )
    BEGIN
        SELECT
            ad.TC                                             AS Tc,
            ad.CHCID                                          AS ChcId,
            COALESCE(NULLIF(ad.BillHead, ''), chcDef.CHCHead, ad.CHCID, '') AS BillHead,
            COALESCE(NULLIF(ad.CHCHeadDescription, ''), chcDef.CHCHeadDescription, '') AS ChcHeadDescription,
            ISNULL(ad.BillAmount, 0)                          AS Amount,
            CASE 
                WHEN ad.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT)
                WHEN ad.IsReceiptable IN ('N', '0', 'False', 'false') THEN CAST(0 AS BIT)
                WHEN chcDef.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT)
                ELSE CAST(0 AS BIT)
            END                                               AS IsReceiptable
        FROM dbo.CF_BillAdjustDetails ad WITH (NOLOCK)
        JOIN dbo.CF_BillAdjustmentEntry ae WITH (NOLOCK) ON ad.AdjustmentNo = ae.AdjustmentNo
        LEFT JOIN dbo.CustomerHandilingCharge chcDef WITH (NOLOCK) ON ad.CHCID = chcDef.CHCID
        WHERE ae.BillNo = @BillNo
          AND ae.AdjustmentNo = (
              SELECT TOP 1 AdjustmentNo 
              FROM dbo.CF_BillAdjustmentEntry WITH (NOLOCK) 
              WHERE BillNo = @BillNo 
              ORDER BY TC DESC
          )
        ORDER BY ad.TC ASC;
    END
    ELSE IF EXISTS (SELECT 1 FROM dbo.CF_BillEntryDetails_CHCHead_ForBillAdjust WITH (NOLOCK) WHERE BillNo = @BillNo)
    BEGIN
        SELECT
            chc.Tc,
            chc.Chcid                                       AS ChcId,
            ISNULL(chc.BillHead, '')                        AS BillHead,
            ISNULL(chc.ChcheadDescription, '')              AS ChcHeadDescription,
            ISNULL(chc.BillAmount, 0)                       AS Amount,
            CASE WHEN chc.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsReceiptable
        FROM dbo.CF_BillEntryDetails_CHCHead_ForBillAdjust chc WITH (NOLOCK)
        WHERE chc.BillNo = @BillNo
        ORDER BY chc.Tc ASC;
    END
    ELSE
    BEGIN
        SELECT
            chc.TC                                          AS Tc,
            chc.CHCID                                       AS ChcId,
            COALESCE(NULLIF(chc.BillHead, ''), chcDef.CHCHead, chc.CHCID, '') AS BillHead,
            COALESCE(NULLIF(chc.CHCHeadDescription, ''), chcDef.CHCHeadDescription, '') AS ChcHeadDescription,
            ISNULL(chc.BillAmount, 0)                       AS Amount,
            CASE WHEN chc.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsReceiptable
        FROM dbo.CF_BillEntryDetails_CHCHead chc WITH (NOLOCK)
        LEFT JOIN dbo.CustomerHandilingCharge chcDef WITH (NOLOCK) ON chc.CHCID = chcDef.CHCID
        WHERE chc.BillNo = @BillNo
        ORDER BY chc.TC ASC;
    END

    -- Result Set 3: Grid 2 - Expenses in Detail (with CF_Def_ExpenseHead join)
    IF EXISTS (SELECT 1 FROM dbo.CF_BillEntryDetails_ForBillAdjust WITH (NOLOCK) WHERE BillNo = @BillNo)
    BEGIN
        SELECT
            expD.Tc,
            ISNULL(expD.BillDetailsId, '')                  AS BillDetailsId,
            expD.BillNo,
            ISNULL(expD.ExpenseHeadId, '')                  AS ExpenseHeadId,
            COALESCE(NULLIF(eh.ExpenseHead, ''), NULLIF(expD.BillHead, ''), expD.ExpenseHeadId, '') AS ExpenseHead,
            ISNULL(expD.EstimatedAmount, 0)                 AS EstimatedAmount,
            ISNULL(expD.ActualAmount, 0)                    AS ActualAmount,
            ISNULL(expD.DifferentAmount, 0)                 AS DifferentAmount,
            CASE WHEN eh.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT) ELSE CAST(1 AS BIT) END AS IsReceiptable
        FROM dbo.CF_BillEntryDetails_ForBillAdjust expD WITH (NOLOCK)
        LEFT JOIN dbo.CF_Def_ExpenseHead eh WITH (NOLOCK) ON expD.ExpenseHeadId = eh.ExpenseHeadID
        WHERE expD.BillNo = @BillNo
        ORDER BY expD.Tc ASC;
    END
    ELSE
    BEGIN
        SELECT
            expD.TC                                         AS Tc,
            ISNULL(expD.BillDetailsID, '')                  AS BillDetailsId,
            expD.BillNo,
            ISNULL(expD.ExpenseHeadID, '')                  AS ExpenseHeadId,
            COALESCE(NULLIF(eh.ExpenseHead, ''), NULLIF(expD.BillHead, ''), expD.ExpenseHeadID, '') AS ExpenseHead,
            ISNULL(expD.EstimatedAmount, 0)                 AS EstimatedAmount,
            ISNULL(expD.ActualAmount, 0)                    AS ActualAmount,
            ISNULL(expD.DifferentAmount, 0)                 AS DifferentAmount,
            CASE WHEN eh.IsReceiptable IN ('Y', '1', 'True', 'true') THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsReceiptable
        FROM dbo.CF_BillEntryDetails expD WITH (NOLOCK)
        LEFT JOIN dbo.CF_Def_ExpenseHead eh WITH (NOLOCK) ON expD.ExpenseHeadID = eh.ExpenseHeadID
        WHERE expD.BillNo = @BillNo
        ORDER BY expD.TC ASC;
    END
END
GO

-- -------------------------------------------------------------------------
-- 5. Stored Procedure: sp_Save_CF_BillAdjustmentEntry
-- -------------------------------------------------------------------------
CREATE OR ALTER PROC dbo.sp_Save_CF_BillAdjustmentEntry
    @AdjustmentNo           NVARCHAR(50)    OUTPUT,
    @AdjustmentType         NVARCHAR(50),
    @AdjustmentDate         DATETIME        = NULL,
    @BillNo                 NVARCHAR(50),
    @CompanyCode            NVARCHAR(50)    = NULL,
    @EmployeeId             NVARCHAR(50)    = NULL,
    @Luser                  NVARCHAR(100)   = NULL,
    @Lip                    NVARCHAR(100)   = NULL,
    @Lmac                   NVARCHAR(150)   = NULL,
    @Remarks                NVARCHAR(MAX)   = NULL,
    @NotesReasonForCredit   NVARCHAR(MAX)   = NULL,
    @DetailTable            dbo.UT_CF_BillAdjustDetails READONLY,
    @DetailJson             NVARCHAR(MAX)   = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @BillNo = LTRIM(RTRIM(ISNULL(@BillNo, '')));
    SET @AdjustmentType = LTRIM(RTRIM(ISNULL(@AdjustmentType, 'Credit Note')));
    SET @AdjustmentNo = LTRIM(RTRIM(ISNULL(@AdjustmentNo, '')));
    SET @CompanyCode = ISNULL(NULLIF(LTRIM(RTRIM(@CompanyCode)), ''), '001');
    SET @EmployeeId = ISNULL(NULLIF(LTRIM(RTRIM(@EmployeeId)), ''), '001');
    SET @Luser = ISNULL(NULLIF(LTRIM(RTRIM(@Luser)), ''), 'Admin');

    IF @BillNo = ''
    BEGIN
        RAISERROR('Bill No is mandatory for Bill Adjustment.', 16, 1);
        RETURN;
    END

    IF @AdjustmentType = ''
        SET @AdjustmentType = 'Credit Note';

    IF @AdjustmentDate IS NULL
        SET @AdjustmentDate = GETDATE();

    CREATE TABLE #DetailsStage
    (
        Chcid               NVARCHAR(50),
        BillHead            NVARCHAR(250),
        CHCHeadDescription  NVARCHAR(500),
        BillAmount          NUMERIC(18, 2),
        IsReceiptable       NVARCHAR(50),
        EmployeeId          NVARCHAR(50)
    );

    IF EXISTS (SELECT 1 FROM @DetailTable)
    BEGIN
        INSERT INTO #DetailsStage (Chcid, BillHead, CHCHeadDescription, BillAmount, IsReceiptable, EmployeeId)
        SELECT 
            t.Chcid, 
            ISNULL(c.CHCHead, t.Chcid),
            c.CHCHeadDescription,
            t.BillAmount, 
            c.IsReceiptable,
            ISNULL(t.EmployeeId, @EmployeeId)
        FROM @DetailTable t
        LEFT JOIN dbo.CustomerHandilingCharge c WITH (NOLOCK) ON t.Chcid = c.CHCID;
    END
    ELSE IF @DetailJson IS NOT NULL AND ISJSON(@DetailJson) = 1
    BEGIN
        INSERT INTO #DetailsStage (Chcid, BillHead, CHCHeadDescription, BillAmount, IsReceiptable, EmployeeId)
        SELECT 
            JSON_VALUE(value, '$.ChcId'),
            COALESCE(NULLIF(JSON_VALUE(value, '$.BillHead'), ''), c.CHCHead, JSON_VALUE(value, '$.ChcId'), ''),
            COALESCE(NULLIF(JSON_VALUE(value, '$.ChcHeadDescription'), ''), c.CHCHeadDescription, ''),
            TRY_CAST(JSON_VALUE(value, '$.Amount') AS NUMERIC(18, 2)),
            CASE 
                WHEN JSON_VALUE(value, '$.IsReceiptable') IN ('true', 'True', '1', 'Y') THEN 'Y'
                ELSE 'N'
            END,
            ISNULL(NULLIF(JSON_VALUE(value, '$.EmployeeId'), ''), @EmployeeId)
        FROM OPENJSON(@DetailJson)
        LEFT JOIN dbo.CustomerHandilingCharge c WITH (NOLOCK) ON JSON_VALUE(value, '$.ChcId') = c.CHCID;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @IsUpdate BIT = 0;

        IF @AdjustmentNo <> '' AND @AdjustmentNo <> '0'
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.CF_BillAdjustmentEntry WHERE AdjustmentNo = @AdjustmentNo)
                SET @IsUpdate = 1;
        END

        IF @IsUpdate = 0
        BEGIN
            DECLARE @Prefix NVARCHAR(10) = CASE 
                                                WHEN @AdjustmentType LIKE '%Debit%' THEN 'DN'
                                                ELSE 'CN'
                                           END;
            DECLARE @YearMonth NVARCHAR(6) = FORMAT(@AdjustmentDate, 'MMyy');
            DECLARE @SearchPattern NVARCHAR(20) = @Prefix + '_' + @YearMonth + '_%';
            DECLARE @LastSeq INT = 0;

            SELECT @LastSeq = ISNULL(MAX(TRY_CAST(RIGHT(AdjustmentNo, 6) AS INT)), 0)
            FROM dbo.CF_BillAdjustmentEntry WITH (UPDLOCK, HOLDLOCK)
            WHERE AdjustmentNo LIKE @SearchPattern;

            SET @AdjustmentNo = @Prefix + '_' + @YearMonth + '_' + RIGHT('000000' + CAST(@LastSeq + 1 AS VARCHAR(6)), 6);
        END

        -- Header Upsert
        IF @IsUpdate = 1
        BEGIN
            UPDATE dbo.CF_BillAdjustmentEntry
            SET AdjustmentType       = @AdjustmentType,
                AdjustmentDate       = @AdjustmentDate,
                BillNo               = @BillNo,
                Remarks              = @Remarks,
                NotesReasonForCredit = @NotesReasonForCredit,
                ModifyDate           = GETDATE(),
                Luser                = @Luser,
                Lip                  = @Lip,
                Lmac                 = @Lmac,
                CompanyCode          = ISNULL(@CompanyCode, CompanyCode),
                EmployeeId           = ISNULL(@EmployeeId, EmployeeId)
            WHERE AdjustmentNo = @AdjustmentNo;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.CF_BillAdjustmentEntry
            (
                AdjustmentNo,
                AdjustmentType,
                AdjustmentDate,
                BillNo,
                Remarks,
                NotesReasonForCredit,
                Luser,
                Ldate,
                Lip,
                Lmac,
                CompanyCode,
                EmployeeId
            )
            VALUES
            (
                @AdjustmentNo,
                @AdjustmentType,
                @AdjustmentDate,
                @BillNo,
                @Remarks,
                @NotesReasonForCredit,
                @Luser,
                GETDATE(),
                @Lip,
                @Lmac,
                @CompanyCode,
                @EmployeeId
            );
        END

        -- Details Overwrite in CF_BillAdjustDetails
        DELETE FROM dbo.CF_BillAdjustDetails
        WHERE AdjustmentNo = @AdjustmentNo;

        INSERT INTO dbo.CF_BillAdjustDetails
        (
            AdjustmentNo,
            Chcid,
            BillHead,
            CHCHeadDescription,
            BillAmount,
            IsReceiptable,
            EmployeeId
        )
        SELECT
            @AdjustmentNo,
            s.Chcid,
            s.BillHead,
            s.CHCHeadDescription,
            ISNULL(s.BillAmount, 0),
            s.IsReceiptable,
            ISNULL(s.EmployeeId, @EmployeeId)
        FROM #DetailsStage s
        WHERE LTRIM(RTRIM(ISNULL(s.Chcid, ''))) <> '';

        -- Sync with CF_BillEntryDetails_CHCHead_ForBillAdjust
        DELETE FROM dbo.CF_BillEntryDetails_CHCHead_ForBillAdjust
        WHERE BillNo = @BillNo;

        INSERT INTO dbo.CF_BillEntryDetails_CHCHead_ForBillAdjust
        (
            BillNo,
            JobNo,
            CHCID,
            BillHead,
            CHCHeadDescription,
            BillAmount,
            IsReceiptable,
            CompanyCode,
            EmployeeID,
            LUser,
            LDate,
            LIP,
            LMAC
        )
        SELECT
            @BillNo,
            @BillNo,
            s.Chcid,
            s.BillHead,
            s.CHCHeadDescription,
            ISNULL(s.BillAmount, 0),
            s.IsReceiptable,
            @CompanyCode,
            @EmployeeId,
            @Luser,
            GETDATE(),
            @Lip,
            @Lmac
        FROM #DetailsStage s
        WHERE LTRIM(RTRIM(ISNULL(s.Chcid, ''))) <> '';

        COMMIT TRANSACTION;

        SELECT 
            1 AS Success,
            @AdjustmentNo AS AdjustmentNo,
            'Bill Adjustment ' + @AdjustmentNo + ' saved successfully.' AS Message;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO
