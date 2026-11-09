USE [GCTL_ERP_DB_Copy];
GO

-- =========================================================================
-- HIGH PERFORMANCE STORED PROCEDURES WITH 7 OPERATIONAL/FINANCIAL COLUMNS
-- =========================================================================

-- 1. Fast Air SP: Proc_CustomerJobStatusFastAir
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusFastAir]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = '',
    @JobStatus      nvarchar(50) = '',
    @BranchCode     nvarchar(50) = ''
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

        -- 7 Operational & Financial Columns after B/E Dt
        ISNULL(ofr.OFRNo, '') AS RequisitionNo,
        CASE 
            WHEN ofr.OFRDate IS NOT NULL AND ofr.OFRDate <> '1900-01-01' 
            THEN CONVERT(varchar(50), ofr.OFRDate, 103) 
            ELSE '' 
        END AS RequisitionDate,
        CASE 
            WHEN ofr.ApprovedUser IS NOT NULL AND LTRIM(RTRIM(ofr.ApprovedUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRApprovalReport,
        CASE 
            WHEN ofr.BillAdjustApprovalUser IS NOT NULL AND LTRIM(RTRIM(ofr.BillAdjustApprovalUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRAdjustApprovalReport,
        ISNULL(v.VoucherNo, '') AS VoucherApproval,
        COALESCE(NULLIF(b.BillNo, ''), NULLIF(d.BillNo, ''), '') AS BillNo,
        CASE 
            WHEN b.BillDate IS NOT NULL AND b.BillDate <> '1900-01-01' THEN CONVERT(varchar(50), b.BillDate, 103)
            ELSE '' 
        END AS BillDate,

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
    LEFT JOIN HRM_Employee emp WITH (NOLOCK) 
        ON d.EmployeeId = emp.EmployeeID 
        OR (LEN(d.EmployeeId) >= 4 AND emp.EmployeeID LIKE '%' + d.EmployeeId)
    OUTER APPLY (
        SELECT TOP 1 
            ofr.OFRNo, ofr.OFRDate, ofr.ApprovedUser, ofr.BillAdjustApprovalUser
        FROM CF_OperationalFundRequisitionEntry ofr WITH (NOLOCK)
        WHERE ofr.JobNo = d.JobNo
        ORDER BY ofr.TC DESC
    ) ofr
    OUTER APPLY (
        SELECT TOP 1 
            b.BillNo, b.BillDate
        FROM CF_BillEntry b WITH (NOLOCK)
        WHERE b.JobNo = d.JobNo
        ORDER BY b.TC DESC
    ) b
    OUTER APPLY (
        SELECT TOP 1 
            v.VoucherNo, v.IsApproved
        FROM Acc_VoucherEntry v WITH (NOLOCK)
        WHERE v.InvoiceNo = d.JobNo 
           OR (b.BillNo IS NOT NULL AND b.BillNo <> '' AND v.InvoiceNo = b.BillNo)
           OR (d.BillNo IS NOT NULL AND d.BillNo <> '' AND v.InvoiceNo = d.BillNo)
        ORDER BY v.autoId DESC
    ) v
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
      AND (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId)
      AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
      AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
      AND (
          ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
          OR COALESCE(
              emp.BranchCode,
              CASE 
                  WHEN d.JobNo LIKE '%Ctg%' THEN '02'
                  WHEN d.JobNo LIKE '%Dha%' OR d.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                  WHEN d.JobNo LIKE '%Pangaon%' THEN '04'
                  ELSE '01'
              END
          ) = @BranchCode
      )
      AND (
          ISNULL(@JobStatus, '') = '' 
          OR (@JobStatus = 'Pending' AND ISNULL(d.ShipmentStatus, '') = '')
          OR (@JobStatus IN ('In Transit', 'transit') AND ISNULL(d.ShipmentStatus, '') IN ('01','02','03','04','05'))
          OR (@JobStatus = 'Delivered' AND ISNULL(d.ShipmentStatus, '') = '06')
      )
      AND (e.ExpenseType LIKE '%Air%' OR ISNULL(d.HAWB, '') <> '' OR ISNULL(d.MAWB, '') <> '' OR d.ExpenseTypeID IN ('0001','0002','0003'))
    ORDER BY d.JobNo ASC;
END
GO

-- 2. Fast Sea SP: Proc_CustomerJobStatusFastSea
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusFastSea]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = '',
    @JobStatus      nvarchar(50) = '',
    @BranchCode     nvarchar(50) = ''
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

        -- 7 Operational & Financial Columns after B/E Dt
        ISNULL(ofr.OFRNo, '') AS RequisitionNo,
        CASE 
            WHEN ofr.OFRDate IS NOT NULL AND ofr.OFRDate <> '1900-01-01' 
            THEN CONVERT(varchar(50), ofr.OFRDate, 103) 
            ELSE '' 
        END AS RequisitionDate,
        CASE 
            WHEN ofr.ApprovedUser IS NOT NULL AND LTRIM(RTRIM(ofr.ApprovedUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRApprovalReport,
        CASE 
            WHEN ofr.BillAdjustApprovalUser IS NOT NULL AND LTRIM(RTRIM(ofr.BillAdjustApprovalUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRAdjustApprovalReport,
        ISNULL(v.VoucherNo, '') AS VoucherApproval,
        COALESCE(NULLIF(b.BillNo, ''), NULLIF(d.BillNo, ''), '') AS JobBillNo,
        CASE 
            WHEN b.BillDate IS NOT NULL AND b.BillDate <> '1900-01-01' THEN CONVERT(varchar(50), b.BillDate, 103)
            ELSE '' 
        END AS BillDate,

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
    LEFT JOIN HRM_Employee emp WITH (NOLOCK) 
        ON d.EmployeeId = emp.EmployeeID 
        OR (LEN(d.EmployeeId) >= 4 AND emp.EmployeeID LIKE '%' + d.EmployeeId)
    OUTER APPLY (
        SELECT TOP 1 
            ofr.OFRNo, ofr.OFRDate, ofr.ApprovedUser, ofr.BillAdjustApprovalUser
        FROM CF_OperationalFundRequisitionEntry ofr WITH (NOLOCK)
        WHERE ofr.JobNo = d.JobNo
        ORDER BY ofr.TC DESC
    ) ofr
    OUTER APPLY (
        SELECT TOP 1 
            b.BillNo, b.BillDate
        FROM CF_BillEntry b WITH (NOLOCK)
        WHERE b.JobNo = d.JobNo
        ORDER BY b.TC DESC
    ) b
    OUTER APPLY (
        SELECT TOP 1 
            v.VoucherNo, v.IsApproved
        FROM Acc_VoucherEntry v WITH (NOLOCK)
        WHERE v.InvoiceNo = d.JobNo 
           OR (b.BillNo IS NOT NULL AND b.BillNo <> '' AND v.InvoiceNo = b.BillNo)
           OR (d.BillNo IS NOT NULL AND d.BillNo <> '' AND v.InvoiceNo = d.BillNo)
        ORDER BY v.autoId DESC
    ) v
    WHERE (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
      AND (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId)
      AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
      AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
      AND (
          ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
          OR COALESCE(
              emp.BranchCode,
              CASE 
                  WHEN d.JobNo LIKE '%Ctg%' THEN '02'
                  WHEN d.JobNo LIKE '%Dha%' OR d.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                  WHEN d.JobNo LIKE '%Pangaon%' THEN '04'
                  ELSE '01'
              END
          ) = @BranchCode
      )
      AND (
          ISNULL(@JobStatus, '') = '' 
          OR (@JobStatus = 'Pending' AND ISNULL(d.ShipmentStatus, '') = '')
          OR (@JobStatus IN ('In Transit', 'transit') AND ISNULL(d.ShipmentStatus, '') IN ('07','08','09','10','11','12','13'))
          OR (@JobStatus = 'Delivered' AND ISNULL(d.ShipmentStatus, '') = '14')
      )
      AND (e.ExpenseType IS NULL OR e.ExpenseType NOT LIKE '%Air%')
    ORDER BY d.JobNo ASC;
END
GO

-- 3. Proc_CustomerJobStatusPendingExceptAll (Air Pending)
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusPendingExceptAll]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = '',
    @BranchCode     nvarchar(50) = ''
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
        LEFT JOIN HRM_Employee emp WITH (NOLOCK) 
            ON d.EmployeeId = emp.EmployeeID 
            OR (LEN(d.EmployeeId) >= 4 AND emp.EmployeeID LIKE '%' + d.EmployeeId)
        WHERE (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId) 
          AND (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
          AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
          AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
          AND (
              ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
              OR COALESCE(
                  emp.BranchCode,
                  CASE 
                      WHEN d.JobNo LIKE '%Ctg%' THEN '02'
                      WHEN d.JobNo LIKE '%Dha%' OR d.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                      WHEN d.JobNo LIKE '%Pangaon%' THEN '04'
                      ELSE '01'
                  END
              ) = @BranchCode
          )
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

        -- 7 Operational & Financial Columns after B/E Dt
        ISNULL(ofr.OFRNo, '') AS RequisitionNo,
        CASE WHEN ofr.OFRDate IS NOT NULL AND ofr.OFRDate <> '1900-01-01' THEN CONVERT(varchar(50), ofr.OFRDate, 103) ELSE '' END AS RequisitionDate,
        CASE 
            WHEN ofr.ApprovedUser IS NOT NULL AND LTRIM(RTRIM(ofr.ApprovedUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRApprovalReport,
        CASE 
            WHEN ofr.BillAdjustApprovalUser IS NOT NULL AND LTRIM(RTRIM(ofr.BillAdjustApprovalUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRAdjustApprovalReport,
        ISNULL(v.VoucherNo, '') AS VoucherApproval,
        COALESCE(NULLIF(b.BillNo, ''), NULLIF(fj.BillNo, ''), '') AS BillNo,
        CASE WHEN b.BillDate IS NOT NULL AND b.BillDate <> '1900-01-01' THEN CONVERT(varchar(50), b.BillDate, 103) ELSE '' END AS BillDate,

        CASE WHEN u.ActualDeliveryDate IS NOT NULL AND u.ActualDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ActualDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.ActualDeliveryDate, 108) ELSE '' END AS ActualDeliveryDate,
        fj.Remarks,
        fj.CustomerID,
        ISNULL(fj.CustomerName, 'General Customer') AS CustomerName
    FROM FilteredJobs fj
    LEFT JOIN Updates u ON fj.JobNo = u.JobNo
    OUTER APPLY (
        SELECT TOP 1 
            ofr.OFRNo, ofr.OFRDate, ofr.ApprovedUser, ofr.BillAdjustApprovalUser
        FROM CF_OperationalFundRequisitionEntry ofr WITH (NOLOCK)
        WHERE ofr.JobNo = fj.JobNo
        ORDER BY ofr.TC DESC
    ) ofr
    OUTER APPLY (
        SELECT TOP 1 
            b.BillNo, b.BillDate
        FROM CF_BillEntry b WITH (NOLOCK)
        WHERE b.JobNo = fj.JobNo
        ORDER BY b.TC DESC
    ) b
    OUTER APPLY (
        SELECT TOP 1 
            v.VoucherNo, v.IsApproved
        FROM Acc_VoucherEntry v WITH (NOLOCK)
        WHERE v.InvoiceNo = fj.JobNo 
           OR (b.BillNo IS NOT NULL AND b.BillNo <> '' AND v.InvoiceNo = b.BillNo)
           OR (fj.BillNo IS NOT NULL AND fj.BillNo <> '' AND v.InvoiceNo = fj.BillNo)
        ORDER BY v.autoId DESC
    ) v
    ORDER BY fj.JobNo ASC;
END
GO

-- 4. Proc_CustomerJobStatusInTransitExceptAll (Air In Transit)
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusInTransitExceptAll]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = '',
    @BranchCode     nvarchar(50) = ''
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
        LEFT JOIN HRM_Employee emp WITH (NOLOCK) 
            ON d.EmployeeId = emp.EmployeeID 
            OR (LEN(d.EmployeeId) >= 4 AND emp.EmployeeID LIKE '%' + d.EmployeeId)
        WHERE (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId) 
          AND (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
          AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
          AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
          AND (
              ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
              OR COALESCE(
                  emp.BranchCode,
                  CASE 
                      WHEN d.JobNo LIKE '%Ctg%' THEN '02'
                      WHEN d.JobNo LIKE '%Dha%' OR d.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                      WHEN d.JobNo LIKE '%Pangaon%' THEN '04'
                      ELSE '01'
                  END
              ) = @BranchCode
          )
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

        -- 7 Operational & Financial Columns after B/E Dt
        ISNULL(ofr.OFRNo, '') AS RequisitionNo,
        CASE WHEN ofr.OFRDate IS NOT NULL AND ofr.OFRDate <> '1900-01-01' THEN CONVERT(varchar(50), ofr.OFRDate, 103) ELSE '' END AS RequisitionDate,
        CASE 
            WHEN ofr.ApprovedUser IS NOT NULL AND LTRIM(RTRIM(ofr.ApprovedUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRApprovalReport,
        CASE 
            WHEN ofr.BillAdjustApprovalUser IS NOT NULL AND LTRIM(RTRIM(ofr.BillAdjustApprovalUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRAdjustApprovalReport,
        ISNULL(v.VoucherNo, '') AS VoucherApproval,
        COALESCE(NULLIF(b.BillNo, ''), NULLIF(fj.BillNo, ''), '') AS BillNo,
        CASE WHEN b.BillDate IS NOT NULL AND b.BillDate <> '1900-01-01' THEN CONVERT(varchar(50), b.BillDate, 103) ELSE '' END AS BillDate,

        CASE WHEN u.ActualDeliveryDate IS NOT NULL AND u.ActualDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ActualDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.ActualDeliveryDate, 108) ELSE '' END AS ActualDeliveryDate,
        fj.Remarks,
        fj.CustomerID,
        ISNULL(fj.CustomerName, 'General Customer') AS CustomerName
    FROM FilteredJobs fj
    LEFT JOIN Updates u ON fj.JobNo = u.JobNo
    OUTER APPLY (
        SELECT TOP 1 
            ofr.OFRNo, ofr.OFRDate, ofr.ApprovedUser, ofr.BillAdjustApprovalUser
        FROM CF_OperationalFundRequisitionEntry ofr WITH (NOLOCK)
        WHERE ofr.JobNo = fj.JobNo
        ORDER BY ofr.TC DESC
    ) ofr
    OUTER APPLY (
        SELECT TOP 1 
            b.BillNo, b.BillDate
        FROM CF_BillEntry b WITH (NOLOCK)
        WHERE b.JobNo = fj.JobNo
        ORDER BY b.TC DESC
    ) b
    OUTER APPLY (
        SELECT TOP 1 
            v.VoucherNo, v.IsApproved
        FROM Acc_VoucherEntry v WITH (NOLOCK)
        WHERE v.InvoiceNo = fj.JobNo 
           OR (b.BillNo IS NOT NULL AND b.BillNo <> '' AND v.InvoiceNo = b.BillNo)
           OR (fj.BillNo IS NOT NULL AND fj.BillNo <> '' AND v.InvoiceNo = fj.BillNo)
        ORDER BY v.autoId DESC
    ) v
    ORDER BY fj.JobNo ASC;
END
GO

-- 5. Proc_CustomerJobStatusDeliveryExceptAll (Air Delivered)
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusDeliveryExceptAll]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = '',
    @BranchCode     nvarchar(50) = ''
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
        LEFT JOIN HRM_Employee emp WITH (NOLOCK) 
            ON d.EmployeeId = emp.EmployeeID 
            OR (LEN(d.EmployeeId) >= 4 AND emp.EmployeeID LIKE '%' + d.EmployeeId)
        WHERE (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId) 
          AND (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
          AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
          AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
          AND (
              ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
              OR COALESCE(
                  emp.BranchCode,
                  CASE 
                      WHEN d.JobNo LIKE '%Ctg%' THEN '02'
                      WHEN d.JobNo LIKE '%Dha%' OR d.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                      WHEN d.JobNo LIKE '%Pangaon%' THEN '04'
                      ELSE '01'
                  END
              ) = @BranchCode
          )
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

        -- 7 Operational & Financial Columns after B/E Dt
        ISNULL(ofr.OFRNo, '') AS RequisitionNo,
        CASE WHEN ofr.OFRDate IS NOT NULL AND ofr.OFRDate <> '1900-01-01' THEN CONVERT(varchar(50), ofr.OFRDate, 103) ELSE '' END AS RequisitionDate,
        CASE 
            WHEN ofr.ApprovedUser IS NOT NULL AND LTRIM(RTRIM(ofr.ApprovedUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRApprovalReport,
        CASE 
            WHEN ofr.BillAdjustApprovalUser IS NOT NULL AND LTRIM(RTRIM(ofr.BillAdjustApprovalUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRAdjustApprovalReport,
        ISNULL(v.VoucherNo, '') AS VoucherApproval,
        COALESCE(NULLIF(b.BillNo, ''), NULLIF(fj.BillNo, ''), '') AS BillNo,
        CASE WHEN b.BillDate IS NOT NULL AND b.BillDate <> '1900-01-01' THEN CONVERT(varchar(50), b.BillDate, 103) ELSE '' END AS BillDate,

        CASE WHEN u.ActualDeliveryDate IS NOT NULL AND u.ActualDeliveryDate <> '1900-01-01' THEN CONVERT(varchar(50), u.ActualDeliveryDate, 103) + ' ' + CONVERT(varchar(5), u.ActualDeliveryDate, 108) ELSE '' END AS ActualDeliveryDate,
        fj.Remarks,
        fj.CustomerID,
        ISNULL(fj.CustomerName, 'General Customer') AS CustomerName
    FROM FilteredJobs fj
    LEFT JOIN Updates u ON fj.JobNo = u.JobNo
    OUTER APPLY (
        SELECT TOP 1 
            ofr.OFRNo, ofr.OFRDate, ofr.ApprovedUser, ofr.BillAdjustApprovalUser
        FROM CF_OperationalFundRequisitionEntry ofr WITH (NOLOCK)
        WHERE ofr.JobNo = fj.JobNo
        ORDER BY ofr.TC DESC
    ) ofr
    OUTER APPLY (
        SELECT TOP 1 
            b.BillNo, b.BillDate
        FROM CF_BillEntry b WITH (NOLOCK)
        WHERE b.JobNo = fj.JobNo
        ORDER BY b.TC DESC
    ) b
    OUTER APPLY (
        SELECT TOP 1 
            v.VoucherNo, v.IsApproved
        FROM Acc_VoucherEntry v WITH (NOLOCK)
        WHERE v.InvoiceNo = fj.JobNo 
           OR (b.BillNo IS NOT NULL AND b.BillNo <> '' AND v.InvoiceNo = b.BillNo)
           OR (fj.BillNo IS NOT NULL AND fj.BillNo <> '' AND v.InvoiceNo = fj.BillNo)
        ORDER BY v.autoId DESC
    ) v
    ORDER BY fj.JobNo ASC;
END
GO

-- 6. Proc_CustomerJobStatusPendingReportForSea (Sea Pending)
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusPendingReportForSea]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = '',
    @BranchCode     nvarchar(50) = ''
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
        LEFT JOIN HRM_Employee emp WITH (NOLOCK) 
            ON d.EmployeeId = emp.EmployeeID 
            OR (LEN(d.EmployeeId) >= 4 AND emp.EmployeeID LIKE '%' + d.EmployeeId)
        WHERE (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId) 
          AND (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
          AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
          AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
          AND (
              ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
              OR COALESCE(
                  emp.BranchCode,
                  CASE 
                      WHEN d.JobNo LIKE '%Ctg%' THEN '02'
                      WHEN d.JobNo LIKE '%Dha%' OR d.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                      WHEN d.JobNo LIKE '%Pangaon%' THEN '04'
                      ELSE '01'
                  END
              ) = @BranchCode
          )
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

        -- 7 Operational & Financial Columns after B/E Dt
        ISNULL(ofr.OFRNo, '') AS RequisitionNo,
        CASE WHEN ofr.OFRDate IS NOT NULL AND ofr.OFRDate <> '1900-01-01' THEN CONVERT(varchar(50), ofr.OFRDate, 103) ELSE '' END AS RequisitionDate,
        CASE 
            WHEN ofr.ApprovedUser IS NOT NULL AND LTRIM(RTRIM(ofr.ApprovedUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRApprovalReport,
        CASE 
            WHEN ofr.BillAdjustApprovalUser IS NOT NULL AND LTRIM(RTRIM(ofr.BillAdjustApprovalUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRAdjustApprovalReport,
        ISNULL(v.VoucherNo, '') AS VoucherApproval,
        COALESCE(NULLIF(b.BillNo, ''), NULLIF(fj.BillNo, ''), '') AS JobBillNo,
        CASE WHEN b.BillDate IS NOT NULL AND b.BillDate <> '1900-01-01' THEN CONVERT(varchar(50), b.BillDate, 103) ELSE '' END AS BillDate,

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
    OUTER APPLY (
        SELECT TOP 1 
            ofr.OFRNo, ofr.OFRDate, ofr.ApprovedUser, ofr.BillAdjustApprovalUser
        FROM CF_OperationalFundRequisitionEntry ofr WITH (NOLOCK)
        WHERE ofr.JobNo = fj.JobNo
        ORDER BY ofr.TC DESC
    ) ofr
    OUTER APPLY (
        SELECT TOP 1 
            b.BillNo, b.BillDate
        FROM CF_BillEntry b WITH (NOLOCK)
        WHERE b.JobNo = fj.JobNo
        ORDER BY b.TC DESC
    ) b
    OUTER APPLY (
        SELECT TOP 1 
            v.VoucherNo, v.IsApproved
        FROM Acc_VoucherEntry v WITH (NOLOCK)
        WHERE v.InvoiceNo = fj.JobNo 
           OR (b.BillNo IS NOT NULL AND b.BillNo <> '' AND v.InvoiceNo = b.BillNo)
           OR (fj.BillNo IS NOT NULL AND fj.BillNo <> '' AND v.InvoiceNo = fj.BillNo)
        ORDER BY v.autoId DESC
    ) v
    ORDER BY fj.JobNo ASC;
END
GO

-- 7. Proc_CustomerJobStatusInTransitSea (Sea In Transit)
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusInTransitSea]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = '',
    @BranchCode     nvarchar(50) = ''
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
        LEFT JOIN HRM_Employee emp WITH (NOLOCK) 
            ON d.EmployeeId = emp.EmployeeID 
            OR (LEN(d.EmployeeId) >= 4 AND emp.EmployeeID LIKE '%' + d.EmployeeId)
        WHERE (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId) 
          AND (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
          AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
          AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
          AND (
              ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
              OR COALESCE(
                  emp.BranchCode,
                  CASE 
                      WHEN d.JobNo LIKE '%Ctg%' THEN '02'
                      WHEN d.JobNo LIKE '%Dha%' OR d.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                      WHEN d.JobNo LIKE '%Pangaon%' THEN '04'
                      ELSE '01'
                  END
              ) = @BranchCode
          )
          AND ISNULL(d.ShipmentStatus, '') IN ('07','08','09','10','11','12','13')
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

        -- 7 Operational & Financial Columns after B/E Dt
        ISNULL(ofr.OFRNo, '') AS RequisitionNo,
        CASE WHEN ofr.OFRDate IS NOT NULL AND ofr.OFRDate <> '1900-01-01' THEN CONVERT(varchar(50), ofr.OFRDate, 103) ELSE '' END AS RequisitionDate,
        CASE 
            WHEN ofr.ApprovedUser IS NOT NULL AND LTRIM(RTRIM(ofr.ApprovedUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRApprovalReport,
        CASE 
            WHEN ofr.BillAdjustApprovalUser IS NOT NULL AND LTRIM(RTRIM(ofr.BillAdjustApprovalUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRAdjustApprovalReport,
        ISNULL(v.VoucherNo, '') AS VoucherApproval,
        COALESCE(NULLIF(b.BillNo, ''), NULLIF(fj.BillNo, ''), '') AS JobBillNo,
        CASE WHEN b.BillDate IS NOT NULL AND b.BillDate <> '1900-01-01' THEN CONVERT(varchar(50), b.BillDate, 103) ELSE '' END AS BillDate,

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
    OUTER APPLY (
        SELECT TOP 1 
            ofr.OFRNo, ofr.OFRDate, ofr.ApprovedUser, ofr.BillAdjustApprovalUser
        FROM CF_OperationalFundRequisitionEntry ofr WITH (NOLOCK)
        WHERE ofr.JobNo = fj.JobNo
        ORDER BY ofr.TC DESC
    ) ofr
    OUTER APPLY (
        SELECT TOP 1 
            b.BillNo, b.BillDate
        FROM CF_BillEntry b WITH (NOLOCK)
        WHERE b.JobNo = fj.JobNo
        ORDER BY b.TC DESC
    ) b
    OUTER APPLY (
        SELECT TOP 1 
            v.VoucherNo, v.IsApproved
        FROM Acc_VoucherEntry v WITH (NOLOCK)
        WHERE v.InvoiceNo = fj.JobNo 
           OR (b.BillNo IS NOT NULL AND b.BillNo <> '' AND v.InvoiceNo = b.BillNo)
           OR (fj.BillNo IS NOT NULL AND fj.BillNo <> '' AND v.InvoiceNo = fj.BillNo)
        ORDER BY v.autoId DESC
    ) v
    ORDER BY fj.JobNo ASC;
END
GO

-- 8. Proc_CustomerJobStatusDeliveredSea (Sea Delivered)
CREATE OR ALTER PROCEDURE [dbo].[Proc_CustomerJobStatusDeliveredSea]
    @DateUse        nvarchar(50) = 'N',
    @FromDate       Datetime = NULL,
    @ToDate         Datetime = NULL,
    @CustomerId     nvarchar(max) = '',
    @PortTypeId     nvarchar(max) = '',
    @ShipmentTypeId nvarchar(max) = '',
    @BranchCode     nvarchar(50) = ''
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
        LEFT JOIN HRM_Employee emp WITH (NOLOCK) 
            ON d.EmployeeId = emp.EmployeeID 
            OR (LEN(d.EmployeeId) >= 4 AND emp.EmployeeID LIKE '%' + d.EmployeeId)
        WHERE (ISNULL(@CustomerId, '') = '' OR d.CustomerID = @CustomerId) 
          AND (@DateUse = 'N' OR (@DateUse = 'Y' AND d.Date BETWEEN @FromDate AND @ToDate))
          AND (ISNULL(@PortTypeId, '') = '' OR p.PortId IN (SELECT Item FROM dbo.SplitString(@PortTypeId, ','))) 
          AND (ISNULL(@ShipmentTypeId, '') = '' OR d.ExpenseTypeID IN (SELECT Item FROM dbo.SplitString(@ShipmentTypeId, ',')))
          AND (
              ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
              OR COALESCE(
                  emp.BranchCode,
                  CASE 
                      WHEN d.JobNo LIKE '%Ctg%' THEN '02'
                      WHEN d.JobNo LIKE '%Dha%' OR d.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                      WHEN d.JobNo LIKE '%Pangaon%' THEN '04'
                      ELSE '01'
                  END
              ) = @BranchCode
          )
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

        -- 7 Operational & Financial Columns after B/E Dt
        ISNULL(ofr.OFRNo, '') AS RequisitionNo,
        CASE WHEN ofr.OFRDate IS NOT NULL AND ofr.OFRDate <> '1900-01-01' THEN CONVERT(varchar(50), ofr.OFRDate, 103) ELSE '' END AS RequisitionDate,
        CASE 
            WHEN ofr.ApprovedUser IS NOT NULL AND LTRIM(RTRIM(ofr.ApprovedUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRApprovalReport,
        CASE 
            WHEN ofr.BillAdjustApprovalUser IS NOT NULL AND LTRIM(RTRIM(ofr.BillAdjustApprovalUser)) <> '' THEN 'Approved'
            WHEN ofr.OFRNo IS NOT NULL AND LTRIM(RTRIM(ofr.OFRNo)) <> '' THEN 'Pending'
            ELSE ''
        END AS OFRAdjustApprovalReport,
        ISNULL(v.VoucherNo, '') AS VoucherApproval,
        COALESCE(NULLIF(b.BillNo, ''), NULLIF(fj.BillNo, ''), '') AS JobBillNo,
        CASE WHEN b.BillDate IS NOT NULL AND b.BillDate <> '1900-01-01' THEN CONVERT(varchar(50), b.BillDate, 103) ELSE '' END AS BillDate,

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
    OUTER APPLY (
        SELECT TOP 1 
            ofr.OFRNo, ofr.OFRDate, ofr.ApprovedUser, ofr.BillAdjustApprovalUser
        FROM CF_OperationalFundRequisitionEntry ofr WITH (NOLOCK)
        WHERE ofr.JobNo = fj.JobNo
        ORDER BY ofr.TC DESC
    ) ofr
    OUTER APPLY (
        SELECT TOP 1 
            b.BillNo, b.BillDate
        FROM CF_BillEntry b WITH (NOLOCK)
        WHERE b.JobNo = fj.JobNo
        ORDER BY b.TC DESC
    ) b
    OUTER APPLY (
        SELECT TOP 1 
            v.VoucherNo, v.IsApproved
        FROM Acc_VoucherEntry v WITH (NOLOCK)
        WHERE v.InvoiceNo = fj.JobNo 
           OR (b.BillNo IS NOT NULL AND b.BillNo <> '' AND v.InvoiceNo = b.BillNo)
           OR (fj.BillNo IS NOT NULL AND fj.BillNo <> '' AND v.InvoiceNo = fj.BillNo)
        ORDER BY v.autoId DESC
    ) v
    ORDER BY fj.JobNo ASC;
END
GO
