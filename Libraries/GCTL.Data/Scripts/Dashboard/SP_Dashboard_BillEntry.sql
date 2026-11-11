CREATE OR ALTER PROCEDURE dbo.SP_Dashboard_BillEntry
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @BranchCode NVARCHAR(50) = '',
    @ShipmentMode NVARCHAR(50) = '',
    @Port NVARCHAR(50) = '',
    @CustomerID NVARCHAR(50) = '',
    @JobStatus NVARCHAR(50) = ''
WITH RECOMPILE
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -3, CAST(GETDATE() AS DATE));
    IF @ToDate IS NULL SET @ToDate = CAST(GETDATE() AS DATE);
    SET @BranchCode = LTRIM(RTRIM(ISNULL(@BranchCode, '')));
    SET @ShipmentMode = LTRIM(RTRIM(ISNULL(@ShipmentMode, '')));
    SET @Port = LTRIM(RTRIM(ISNULL(@Port, '')));
    SET @CustomerID = LTRIM(RTRIM(ISNULL(@CustomerID, '')));
    SET @JobStatus = LTRIM(RTRIM(ISNULL(@JobStatus, '')));

    ---------------------------------------------------------------------
    -- 1. Bills in scope
    ---------------------------------------------------------------------
    SELECT 
        b.BillNo,
        b.JobNo,
        b.BillDate,
        b.CustomerID,
        ISNULL(c.CustomerName, 'Unknown') AS CustomerName,
        b.ExpenseTypeID,
        ISNULL(et.ExpenseType, 'Other') AS ExpenseType,
        ISNULL(b.MaterialDescription, '') AS MaterialDescription,
        b.Quantity,
        b.DocWeight,
        b.QUnitTypeID,
        b.WUnitTypeID,
        ISNULL(b.BillAmount, 0) AS GrossBill,
        ISNULL(b.SpecialDiscount, 0) AS SpecialDiscount,
        (ISNULL(b.BillAmount, 0) - ISNULL(b.SpecialDiscount, 0)) AS NetAmount,
        ISNULL(b.MinimumAgencyCommission, 0) AS MinimumAgencyCommission,
        d.Quntity1 AS DocQty,
        d.Quntity2 AS DocWeightVal,
        d.Unit1 AS DocUnit1,
        d.Unit2 AS DocUnit2
    INTO #Bills
    FROM CF_BillEntry b WITH (NOLOCK)
    LEFT JOIN Sep_Documentation d WITH (NOLOCK) ON d.JobNo = b.JobNo
    LEFT JOIN CF_Def_ExpenseType et WITH (NOLOCK) ON et.ExpenseTypeID = b.ExpenseTypeID
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON c.CustomerID = b.CustomerID
    LEFT JOIN HRM_Employee emp WITH (NOLOCK) 
        ON COALESCE(NULLIF(b.EmployeeId, ''), d.EmployeeId) = emp.EmployeeID 
        OR (LEN(COALESCE(NULLIF(b.EmployeeId, ''), d.EmployeeId)) >= 4 AND emp.EmployeeID LIKE '%' + COALESCE(NULLIF(b.EmployeeId, ''), d.EmployeeId))
    WHERE (b.BillDate >= @FromDate AND b.BillDate <= DATEADD(DAY, 1, @ToDate))
      AND (@CustomerID = '' OR b.CustomerID = @CustomerID)
      AND (@ShipmentMode = '' OR b.ExpenseTypeID = @ShipmentMode)
      AND (
          ISNULL(@BranchCode, '') = '' OR @BranchCode = 'ALL' 
          OR COALESCE(
              emp.BranchCode,
              CASE 
                  WHEN b.JobNo LIKE '%Ctg%' THEN '02'
                  WHEN b.JobNo LIKE '%Dha%' OR b.ExpenseTypeID IN ('0001','0002','0003') THEN '03'
                  WHEN b.JobNo LIKE '%Pangaon%' THEN '04'
                  ELSE '01'
              END
          ) = @BranchCode
      )
      AND (@Port = '' OR d.PortNameId = @Port)
      AND (
          @JobStatus = '' 
          OR d.ShipmentStatus = @JobStatus
          OR (
              CASE 
                  WHEN d.DateOfClearence IS NOT NULL 
                       OR ISNULL(d.ShipmentStatus,'') LIKE '%DELIVER%' 
                       OR ISNULL(d.ShipmentStatus,'') LIKE '%SUCCESS%' 
                       OR d.ShipmentStatus IN ('06', '14') THEN 'Delivered'
                  WHEN d.ETA IS NOT NULL 
                       OR ISNULL(d.BENo,'') <> '' 
                       OR ISNULL(d.ShipmentStatus,'') LIKE '%TRANSIT%' 
                       OR ISNULL(d.ShipmentStatus,'') LIKE '%PROCESSING%' 
                       OR d.ShipmentStatus IN ('01','02','03','04','05','07','08','09','10','11','12','13') THEN 'In Transit'
                  ELSE 'Pending'
              END
          ) = @JobStatus
      );

    CREATE CLUSTERED INDEX IX_Bills_BillNo ON #Bills(BillNo);

    ---------------------------------------------------------------------
    -- 2. Line Items Breakdown (CHC + Expense heads)
    ---------------------------------------------------------------------
    SELECT 
        ch.BillNo,
        SUM(CASE WHEN (ch.BillHead LIKE '%Transport%' OR ch.ChcheadDescription LIKE '%Transport%') THEN ISNULL(ch.BillAmount, 0) ELSE 0 END) AS TransportBill,
        SUM(CASE WHEN (ch.BillHead LIKE '%Agency%' OR ch.ChcheadDescription LIKE '%Agency%') THEN ISNULL(ch.BillAmount, 0) ELSE 0 END) AS AgencyBill,
        SUM(CASE WHEN (ch.BillHead NOT LIKE '%Transport%' AND ch.ChcheadDescription NOT LIKE '%Transport%' 
                      AND ch.BillHead NOT LIKE '%Agency%' AND ch.ChcheadDescription NOT LIKE '%Agency%' 
                      AND COALESCE(NULLIF(ch.IsReceiptable,''), h.IsReceiptable) = 'Y') THEN ISNULL(ch.BillAmount, 0) ELSE 0 END) AS Rec,
        SUM(CASE WHEN (ch.BillHead NOT LIKE '%Transport%' AND ch.ChcheadDescription NOT LIKE '%Transport%' 
                      AND ch.BillHead NOT LIKE '%Agency%' AND ch.ChcheadDescription NOT LIKE '%Agency%' 
                      AND COALESCE(NULLIF(ch.IsReceiptable,''), h.IsReceiptable) <> 'Y') THEN ISNULL(ch.BillAmount, 0) ELSE 0 END) AS NonRec
    INTO #Chc
    FROM CF_BillEntryDetails_CHCHead ch WITH (NOLOCK)
    JOIN #Bills b ON b.BillNo = ch.BillNo
    LEFT JOIN CustomerHandilingCharge h WITH (NOLOCK) ON h.CHCID = ch.CHCID
    GROUP BY ch.BillNo;

    CREATE CLUSTERED INDEX IX_Chc ON #Chc(BillNo);

    SELECT 
        bed.BillNo,
        SUM(CASE WHEN (bed.BillHead LIKE '%Transport%' OR eh.ExpenseHead LIKE '%Transport%') THEN ISNULL(bed.ExpenseBillAmount, ISNULL(bed.ActualAmount, 0)) ELSE 0 END) AS TransportBill,
        SUM(CASE WHEN (bed.BillHead LIKE '%Agency%' OR eh.ExpenseHead LIKE '%Agency%') THEN ISNULL(bed.ExpenseBillAmount, ISNULL(bed.ActualAmount, 0)) ELSE 0 END) AS AgencyBill,
        SUM(CASE WHEN (bed.BillHead NOT LIKE '%Transport%' AND eh.ExpenseHead NOT LIKE '%Transport%' 
                      AND bed.BillHead NOT LIKE '%Agency%' AND eh.ExpenseHead NOT LIKE '%Agency%' 
                      AND eh.IsReceiptable = 'Y') THEN ISNULL(bed.ExpenseBillAmount, ISNULL(bed.ActualAmount, 0)) ELSE 0 END) AS Rec,
        SUM(CASE WHEN (bed.BillHead NOT LIKE '%Transport%' AND eh.ExpenseHead NOT LIKE '%Transport%' 
                      AND bed.BillHead NOT LIKE '%Agency%' AND eh.ExpenseHead NOT LIKE '%Agency%' 
                      AND (eh.IsReceiptable <> 'Y' OR eh.IsReceiptable IS NULL)) THEN ISNULL(bed.ExpenseBillAmount, ISNULL(bed.ActualAmount, 0)) ELSE 0 END) AS NonRec
    INTO #Exp
    FROM CF_BillEntryDetails bed WITH (NOLOCK)
    JOIN #Bills b ON b.BillNo = bed.BillNo
    LEFT JOIN CF_Def_ExpenseHead eh WITH (NOLOCK) ON eh.ExpenseHeadID = bed.ExpenseHeadID
    GROUP BY bed.BillNo;

    CREATE CLUSTERED INDEX IX_Exp ON #Exp(BillNo);

    ---------------------------------------------------------------------
    -- Combine Detailed Billing Row View
    ---------------------------------------------------------------------
    SELECT 
        b.BillNo,
        b.JobNo,
        b.BillDate,
        b.CustomerName,
        b.ExpenseType,
        b.MaterialDescription,
        CASE 
            WHEN COALESCE(b.Quantity, b.DocQty) IS NOT NULL AND COALESCE(b.Quantity, b.DocQty) <> 0 
            THEN LTRIM(RTRIM(STR(COALESCE(b.Quantity, b.DocQty), 18, 2))) + ' ' + ISNULL(COALESCE(uq.UnitTypeName, uq.ShortName, b.QUnitTypeID, b.DocUnit1), '')
            ELSE '-' 
        END AS Quantity,
        CASE 
            WHEN COALESCE(b.DocWeight, b.DocWeightVal) IS NOT NULL AND COALESCE(b.DocWeight, b.DocWeightVal) <> 0 
            THEN LTRIM(RTRIM(STR(COALESCE(b.DocWeight, b.DocWeightVal), 18, 2))) + ' ' + ISNULL(COALESCE(uw.UnitTypeName, uw.ShortName, b.WUnitTypeID, b.DocUnit2), '')
            ELSE '-' 
        END AS Weight,
        ISNULL(chc.Rec, 0) + ISNULL(ex.Rec, 0) AS ReceiptableAmount,
        ISNULL(chc.NonRec, 0) + ISNULL(ex.NonRec, 0) AS NonReceiptableAmount,
        ISNULL(chc.TransportBill, 0) + ISNULL(ex.TransportBill, 0) AS TransportAmount,
        CASE 
            WHEN (ISNULL(chc.AgencyBill, 0) + ISNULL(ex.AgencyBill, 0)) > 0 
            THEN (ISNULL(chc.AgencyBill, 0) + ISNULL(ex.AgencyBill, 0))
            ELSE b.MinimumAgencyCommission 
        END AS AgencyCommission,
        b.GrossBill AS TotalAmount,
        b.SpecialDiscount AS Deduction,
        b.NetAmount,
        CASE WHEN EXISTS (
            SELECT 1 FROM Acc_VoucherEntry v WITH (NOLOCK) 
            WHERE v.PeriodCodeNo = 'From Bill Entry' AND v.InvoiceNo = b.BillNo
        ) THEN 0.00 ELSE b.NetAmount END AS DueAmount,
        CASE WHEN EXISTS (
            SELECT 1 FROM Acc_VoucherEntry v WITH (NOLOCK) 
            WHERE v.PeriodCodeNo = 'From Bill Entry' AND v.InvoiceNo = b.BillNo
        ) THEN b.NetAmount ELSE 0.00 END AS CollectedAmount,
        CASE WHEN EXISTS (
            SELECT 1 FROM Acc_VoucherEntry v WITH (NOLOCK) 
            WHERE v.PeriodCodeNo = 'From Bill Entry' AND v.InvoiceNo = b.BillNo
        ) THEN 'Paid' ELSE 'Due' END AS BillStatus
    INTO #BillFull
    FROM #Bills b
    LEFT JOIN #Chc chc ON chc.BillNo = b.BillNo
    LEFT JOIN #Exp ex ON ex.BillNo = b.BillNo
    LEFT JOIN Rmg_Prod_Def_UnitType uq WITH (NOLOCK) ON uq.UnitTypID = COALESCE(b.QUnitTypeID, b.DocUnit1)
    LEFT JOIN Rmg_Prod_Def_UnitType uw WITH (NOLOCK) ON uw.UnitTypID = COALESCE(b.WUnitTypeID, b.DocUnit2);

    ---------------------------------------------------------------------
    -- RESULT SET 1: KPI Summary
    ---------------------------------------------------------------------
    DECLARE @TotalBills INT, @NetServiceValue NUMERIC(18,2), @TotalDue NUMERIC(18,2);
    DECLARE @LastServiceValue NUMERIC(18,2), @LastServiceDate VARCHAR(20), @LastBillNo NVARCHAR(100);
    DECLARE @Collected NUMERIC(18,2), @CollectionRate NUMERIC(18,2), @AvgBillValue NUMERIC(18,2), @TotalComm NUMERIC(18,2), @TotalDed NUMERIC(18,2);

    SELECT 
        @TotalBills = COUNT(*),
        @NetServiceValue = ISNULL(SUM(NetAmount), 0),
        @TotalDue = ISNULL(SUM(DueAmount), 0),
        @Collected = ISNULL(SUM(CollectedAmount), 0),
        @CollectionRate = CASE WHEN SUM(NetAmount) > 0 THEN ROUND((SUM(CollectedAmount) * 100.0) / SUM(NetAmount), 2) ELSE 0 END,
        @AvgBillValue = CASE WHEN COUNT(*) > 0 THEN ISNULL(SUM(NetAmount), 0) / COUNT(*) ELSE 0 END,
        @TotalComm = ISNULL(SUM(AgencyCommission), 0),
        @TotalDed = ISNULL(SUM(Deduction), 0)
    FROM #BillFull;

    SELECT TOP 1 
        @LastServiceValue = NetAmount,
        @LastServiceDate = CONVERT(VARCHAR(10), BillDate, 103),
        @LastBillNo = BillNo
    FROM #BillFull
    ORDER BY BillDate DESC, BillNo DESC;

    SELECT 
        ISNULL(@TotalBills, 0) AS TotalBills,
        ISNULL(@NetServiceValue, 0) AS NetBillValue,
        ISNULL(@TotalDue, 0) AS TotalOutstandingDue,
        ISNULL(@LastServiceValue, 0) AS LastServiceValue,
        ISNULL(@LastServiceDate, '-') AS LastServiceDate,
        ISNULL(@LastBillNo, '-') AS LastBillNo,
        ISNULL(@Collected, 0) AS TotalCollected,
        ISNULL(@CollectionRate, 0) AS CollectionEfficiency,
        ISNULL(@AvgBillValue, 0) AS AvgBillValue,
        ISNULL(@TotalComm, 0) AS AgencyCommission,
        ISNULL(@TotalDed, 0) AS TotalDeductions;

    ---------------------------------------------------------------------
    -- RESULT SET 2: Service Value Composition
    ---------------------------------------------------------------------
    SELECT 
        'Receivable' AS ServiceHead, ISNULL(SUM(ReceiptableAmount), 0) AS Amount FROM #BillFull
    UNION ALL
    SELECT 
        'Non-Receivable' AS ServiceHead, ISNULL(SUM(NonReceiptableAmount), 0) AS Amount FROM #BillFull
    UNION ALL
    SELECT 
        'Transport' AS ServiceHead, ISNULL(SUM(TransportAmount), 0) AS Amount FROM #BillFull
    UNION ALL
    SELECT 
        'Agency Commission' AS ServiceHead, ISNULL(SUM(AgencyCommission), 0) AS Amount FROM #BillFull;

    ---------------------------------------------------------------------
    -- RESULT SET 3: Net Service Value by Shipment Mode
    ---------------------------------------------------------------------
    SELECT 
        ExpenseType AS ModeName,
        ISNULL(SUM(NetAmount), 0) AS TotalBilling,
        COUNT(*) AS BillCount
    FROM #BillFull
    GROUP BY ExpenseType
    ORDER BY TotalBilling DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 4: Collection Status
    ---------------------------------------------------------------------
    SELECT 'Collected' AS StatusName, ISNULL(@Collected, 0) AS Amount
    UNION ALL
    SELECT 'Outstanding' AS StatusName, ISNULL(@TotalDue, 0) AS Amount;

    ---------------------------------------------------------------------
    -- RESULT SET 5: Monthly Billing Trend
    ---------------------------------------------------------------------
    SELECT 
        FORMAT(BillDate, 'yyyy-MM') AS MonthKey,
        FORMAT(BillDate, 'MMM yy') AS MonthLabel,
        ISNULL(SUM(NetAmount), 0) AS BilledAmount,
        ISNULL(SUM(CollectedAmount), 0) AS CollectedAmount,
        ISNULL(SUM(DueAmount), 0) AS DueAmount
    FROM #BillFull
    GROUP BY FORMAT(BillDate, 'yyyy-MM'), FORMAT(BillDate, 'MMM yy')
    ORDER BY MonthKey ASC;

    ---------------------------------------------------------------------
    -- RESULT SET 6: Top Customers by Billing
    ---------------------------------------------------------------------
    SELECT TOP 7
        CustomerName,
        ISNULL(SUM(NetAmount), 0) AS TotalBilled
    FROM #BillFull
    GROUP BY CustomerName
    ORDER BY TotalBilled DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 7: Bill List (Detailed Table like JobBillEntryReport)
    ---------------------------------------------------------------------
    SELECT 
        ROW_NUMBER() OVER (ORDER BY BillDate DESC, BillNo DESC) AS SL,
        BillNo,
        CONVERT(VARCHAR(10), BillDate, 103) AS BillDate,
        JobNo,
        CustomerName,
        ExpenseType,
        MaterialDescription,
        Quantity,
        Weight,
        ReceiptableAmount,
        NonReceiptableAmount,
        TransportAmount,
        AgencyCommission,
        TotalAmount,
        Deduction,
        NetAmount,
        DueAmount,
        CollectedAmount,
        BillStatus
    FROM #BillFull
    ORDER BY BillDate DESC, BillNo DESC;

    DROP TABLE #Bills;
    DROP TABLE #Chc;
    DROP TABLE #Exp;
    DROP TABLE #BillFull;
END;
GO
