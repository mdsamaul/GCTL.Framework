IF OBJECT_ID('dbo.SP_Dashboard_GPAnalysis', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_Dashboard_GPAnalysis;
GO

CREATE PROCEDURE dbo.SP_Dashboard_GPAnalysis
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @BranchCode NVARCHAR(50) = '',
    @ShipmentMode NVARCHAR(50) = '',
    @CustomerID NVARCHAR(50) = ''
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL SET @FromDate = DATEADD(MONTH, -3, CAST(GETDATE() AS DATE));
    IF @ToDate IS NULL SET @ToDate = CAST(GETDATE() AS DATE);
    SET @BranchCode = LTRIM(RTRIM(ISNULL(@BranchCode, '')));
    SET @ShipmentMode = LTRIM(RTRIM(ISNULL(@ShipmentMode, '')));
    SET @CustomerID = LTRIM(RTRIM(ISNULL(@CustomerID, '')));

    ---------------------------------------------------------------------
    -- 1. Bills in scope
    ---------------------------------------------------------------------
    SELECT 
        be.BillNo,
        be.JobNo,
        CAST(be.BillDate AS DATE) AS BillDate,
        d.CustomerID,
        ISNULL(c.CustomerName, 'Unknown') AS CustomerName,
        d.ExpenseTypeID,
        ISNULL(et.ExpenseType, 'Other') AS ExpenseType,
        CASE 
            WHEN x.Calc > be.MinimumAgencyCommission THEN x.Calc
            ELSE ISNULL(be.MinimumAgencyCommission, 0)
        END AS Commission
    INTO #Bills
    FROM CF_BillEntry be WITH (NOLOCK)
    JOIN Sep_Documentation d WITH (NOLOCK) ON d.JobNo = be.JobNo
    LEFT JOIN CF_Def_ExpenseType et WITH (NOLOCK) ON et.ExpenseTypeID = d.ExpenseTypeID
    LEFT JOIN Sales_Customer c WITH (NOLOCK) ON c.CustomerID = d.CustomerID
    CROSS APPLY (SELECT CAST(
            ISNULL(be.OnInvoiceValue, 0)
            * CASE WHEN be.LCValue IS NULL OR be.LCValue = 0 THEN ISNULL(d.LCValue, 0) ELSE be.LCValue END
            * ISNULL(be.FOBValueBDT, 1) / 100.0 AS DECIMAL(18,2)) AS Calc) x
    WHERE (be.BillDate >= @FromDate AND be.BillDate <= DATEADD(DAY, 1, @ToDate))
      AND (@CustomerID = '' OR d.CustomerID = @CustomerID)
      AND (@ShipmentMode = '' OR d.ExpenseTypeID = @ShipmentMode);

    CREATE CLUSTERED INDEX IX_Bills_BillNo ON #Bills(BillNo);
    CREATE INDEX IX_Bills_JobNo ON #Bills(JobNo);

    ---------------------------------------------------------------------
    -- 2. CHC billing
    ---------------------------------------------------------------------
    SELECT 
        ch.BillNo,
        SUM(CASE WHEN ch.CHCID IN ('00009','00021') THEN ch.BillAmount ELSE 0 END) AS TransportBill,
        SUM(CASE WHEN ch.CHCID NOT IN ('00009','00021') AND f.Flag = 'Y' THEN ch.BillAmount ELSE 0 END) AS Rec,
        SUM(CASE WHEN ch.CHCID NOT IN ('00009','00021') AND f.Flag = 'N' THEN ch.BillAmount ELSE 0 END) AS NonRec
    INTO #Chc
    FROM CF_BillEntryDetails_CHCHead ch WITH (NOLOCK)
    JOIN #Bills b ON b.BillNo = ch.BillNo
    JOIN CustomerHandilingCharge h WITH (NOLOCK) ON h.CHCID = ch.CHCID
    CROSS APPLY (SELECT CASE WHEN COALESCE(NULLIF(ch.IsReceiptable,''), h.IsReceiptable) = 'Y' THEN 'Y' ELSE 'N' END AS Flag) f
    GROUP BY ch.BillNo;

    CREATE CLUSTERED INDEX IX_Chc ON #Chc(BillNo);

    ---------------------------------------------------------------------
    -- 3. Expense-head billing
    ---------------------------------------------------------------------
    SELECT 
        bed.BillNo,
        SUM(CASE WHEN eh.IsReceiptable = 'Y' THEN ISNULL(bed.ExpenseBillAmount,0) ELSE 0 END) AS Rec,
        SUM(CASE WHEN eh.IsReceiptable = 'N' OR eh.IsReceiptable IS NULL THEN ISNULL(bed.ExpenseBillAmount,0) ELSE 0 END) AS NonRec
    INTO #Exp
    FROM CF_BillEntryDetails bed WITH (NOLOCK)
    JOIN #Bills b ON b.BillNo = bed.BillNo
    LEFT JOIN CF_Def_ExpenseHead eh WITH (NOLOCK) ON eh.ExpenseHeadID = bed.ExpenseHeadID
    GROUP BY bed.BillNo;

    CREATE CLUSTERED INDEX IX_Exp ON #Exp(BillNo);

    ---------------------------------------------------------------------
    -- 4. Costing from Requisition Details
    ---------------------------------------------------------------------
    SELECT 
        bd.JobNo,
        SUM(CASE WHEN bd.ServiceTypeID <> '0003' AND beh.IsReceiptable = 'Y' THEN ISNULL(bd.BillAdjustAmount,0) ELSE 0 END) AS CostRec,
        SUM(CASE WHEN bd.ServiceTypeID <> '0003' AND (beh.IsReceiptable = 'N' OR beh.IsReceiptable IS NULL) THEN ISNULL(bd.BillAdjustAmount,0) ELSE 0 END) AS CostNonRec,
        SUM(CASE WHEN bd.ServiceTypeID = '0003' THEN ISNULL(bd.BillAdjustAmount,0) ELSE 0 END) AS CostTransport
    INTO #Cost
    FROM #Bills b
    INNER JOIN CF_OperationalFundRequisitionDetails bd WITH (NOLOCK) ON bd.JobNo = b.JobNo
    LEFT JOIN CF_Def_ExpenseHead beh WITH (NOLOCK) ON beh.ExpenseHeadID = bd.ExpenseHeadID
    GROUP BY bd.JobNo;

    CREATE CLUSTERED INDEX IX_Cost ON #Cost(JobNo);

    ---------------------------------------------------------------------
    -- 5. Consolidated GP Row Dataset
    ---------------------------------------------------------------------
    SELECT 
        b.ExpenseType,
        b.ExpenseTypeID,
        b.CustomerName,
        b.CustomerID,
        b.JobNo,
        b.BillNo,
        b.BillDate,
        b.Commission,
        a.BillingTransportBill,
        a.BillingReceptableAmount,
        a.BillingNonReceptableAmount,
        a.CostingReceptableAmount,
        a.CostingNonReceptableAmount,
        a.CostingTransport,
        t.OthersBill,
        t.TotalCNFBilling,
        t.CNFCosting,
        t.TotalBilling,
        t.TotalCosting,
        g.TotalGP,
        g.CNFGP,
        g.TransportGP,
        CASE WHEN t.TotalBilling > 0 THEN ROUND(g.TotalGP * 100.0 / t.TotalBilling, 2) ELSE 0 END AS TotalGPPercent,
        CASE WHEN t.TotalCNFBilling > 0 THEN ROUND(g.CNFGP * 100.0 / t.TotalCNFBilling, 2) ELSE 0 END AS CNFGPPercent,
        CASE WHEN a.BillingTransportBill > 0 THEN ROUND(g.TransportGP * 100.0 / a.BillingTransportBill, 2) ELSE 0 END AS TransportGPPercent
    INTO #GPRows
    FROM #Bills b
    LEFT JOIN #Chc chc ON chc.BillNo = b.BillNo
    LEFT JOIN #Exp ex ON ex.BillNo = b.BillNo
    LEFT JOIN #Cost cst ON cst.JobNo = b.JobNo
    CROSS APPLY (SELECT
            ISNULL(chc.TransportBill,0) AS BillingTransportBill,
            ISNULL(chc.Rec,0) + ISNULL(ex.Rec,0) AS BillingReceptableAmount,
            ISNULL(chc.NonRec,0) + ISNULL(ex.NonRec,0) AS BillingNonReceptableAmount,
            ISNULL(cst.CostRec,0) AS CostingReceptableAmount,
            ISNULL(cst.CostNonRec,0) AS CostingNonReceptableAmount,
            ISNULL(cst.CostTransport,0) AS CostingTransport) a
    CROSS APPLY (SELECT
            a.BillingReceptableAmount + a.BillingNonReceptableAmount AS OthersBill,
            a.BillingReceptableAmount + a.BillingNonReceptableAmount + b.Commission AS TotalCNFBilling,
            a.CostingReceptableAmount + a.CostingNonReceptableAmount AS CNFCosting) t0
    CROSS APPLY (SELECT
            t0.OthersBill,
            t0.TotalCNFBilling,
            t0.CNFCosting,
            a.CostingTransport,
            a.BillingTransportBill + t0.TotalCNFBilling AS TotalBilling,
            t0.CNFCosting + a.CostingTransport AS TotalCosting) t
    CROSS APPLY (SELECT
            t.TotalBilling - t.TotalCosting AS TotalGP,
            t.TotalCNFBilling - t.CNFCosting AS CNFGP,
            a.BillingTransportBill - a.CostingTransport AS TransportGP) g;

    ---------------------------------------------------------------------
    -- RESULT SET 1: KPI Summary & Minis
    ---------------------------------------------------------------------
    DECLARE @TotalBills INT, @NetBillValue NUMERIC(18,2), @TotalCosting NUMERIC(18,2), @GrossProfit NUMERIC(18,2), @GPMargin NUMERIC(18,2);
    DECLARE @CNFProfit NUMERIC(18,2), @TransportProfit NUMERIC(18,2);
    DECLARE @AirExportProfit NUMERIC(18,2), @AirExportBilling NUMERIC(18,2), @AirExportGPPercent NUMERIC(18,2);
    DECLARE @BestMode NVARCHAR(100), @BestModeGP NUMERIC(18,2), @LowestMode NVARCHAR(100), @LowestModeGP NUMERIC(18,2);
    DECLARE @AvgProfitPerBill NUMERIC(18,2);

    SELECT 
        @TotalBills = COUNT(*),
        @NetBillValue = ISNULL(SUM(TotalBilling), 0),
        @TotalCosting = ISNULL(SUM(TotalCosting), 0),
        @GrossProfit = ISNULL(SUM(TotalGP), 0),
        @CNFProfit = ISNULL(SUM(CNFGP), 0),
        @TransportProfit = ISNULL(SUM(TransportGP), 0),
        @GPMargin = CASE WHEN SUM(TotalBilling) > 0 THEN ROUND(SUM(TotalGP) * 100.0 / SUM(TotalBilling), 2) ELSE 0 END,
        @AvgProfitPerBill = CASE WHEN COUNT(*) > 0 THEN ISNULL(SUM(TotalGP), 0) / COUNT(*) ELSE 0 END
    FROM #GPRows;

    -- Air Export Profit & GP%
    SELECT 
        @AirExportBilling = ISNULL(SUM(TotalBilling), 0),
        @AirExportProfit = ISNULL(SUM(TotalGP), 0),
        @AirExportGPPercent = CASE WHEN SUM(TotalBilling) > 0 THEN ROUND(SUM(TotalGP) * 100.0 / SUM(TotalBilling), 2) ELSE 0 END
    FROM #GPRows
    WHERE ExpenseType LIKE '%Air Export%';

    -- Best & Lowest Modes
    ;WITH ModeStats AS (
        SELECT 
            ExpenseType,
            CASE WHEN SUM(TotalBilling) > 0 THEN ROUND(SUM(TotalGP) * 100.0 / SUM(TotalBilling), 2) ELSE 0 END AS ModeGP
        FROM #GPRows
        GROUP BY ExpenseType
    )
    SELECT TOP 1 @BestMode = ExpenseType, @BestModeGP = ModeGP FROM ModeStats ORDER BY ModeGP DESC;

    ;WITH ModeStats AS (
        SELECT 
            ExpenseType,
            CASE WHEN SUM(TotalBilling) > 0 THEN ROUND(SUM(TotalGP) * 100.0 / SUM(TotalBilling), 2) ELSE 0 END AS ModeGP
        FROM #GPRows
        GROUP BY ExpenseType
    )
    SELECT TOP 1 @LowestMode = ExpenseType, @LowestModeGP = ModeGP FROM ModeStats ORDER BY ModeGP ASC;

    SELECT 
        ISNULL(@TotalBills, 0) AS TotalBills,
        ISNULL(@NetBillValue, 0) AS NetBillValue,
        ISNULL(@TotalCosting, 0) AS TotalCosting,
        ISNULL(@GrossProfit, 0) AS GrossProfit,
        ISNULL(@GPMargin, 0) AS GPMargin,
        ISNULL(@CNFProfit, 0) AS CNFProfit,
        ISNULL(@TransportProfit, 0) AS TransportProfit,
        ISNULL(@AirExportProfit, 0) AS AirExportProfit,
        ISNULL(@AirExportGPPercent, 0) AS AirExportGPPercent,
        ISNULL(@BestMode, '-') AS BestMode,
        ISNULL(@BestModeGP, 0) AS BestModeGP,
        ISNULL(@LowestMode, '-') AS LowestMode,
        ISNULL(@LowestModeGP, 0) AS LowestModeGP,
        ISNULL(@AvgProfitPerBill, 0) AS AvgProfitPerBill;

    ---------------------------------------------------------------------
    -- RESULT SET 2: Costing Composition
    ---------------------------------------------------------------------
    SELECT 'C&F Receivable cost' AS CostHead, ISNULL(SUM(CostingReceptableAmount), 0) AS CostAmount FROM #GPRows
    UNION ALL
    SELECT 'C&F Non-receivable cost' AS CostHead, ISNULL(SUM(CostingNonReceptableAmount), 0) AS CostAmount FROM #GPRows
    UNION ALL
    SELECT 'Transport cost' AS CostHead, ISNULL(SUM(CostingTransport), 0) AS CostAmount FROM #GPRows;

    ---------------------------------------------------------------------
    -- RESULT SET 3: GP Margin by Shipment Mode
    ---------------------------------------------------------------------
    SELECT 
        ExpenseType AS ModeName,
        ISNULL(SUM(TotalBilling), 0) AS TotalBilling,
        ISNULL(SUM(TotalCosting), 0) AS TotalCosting,
        ISNULL(SUM(TotalGP), 0) AS TotalProfit,
        CASE WHEN SUM(TotalBilling) > 0 THEN ROUND(SUM(TotalGP) * 100.0 / SUM(TotalBilling), 2) ELSE 0 END AS GPPercent
    FROM #GPRows
    GROUP BY ExpenseType
    ORDER BY TotalProfit DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 4: Billing vs Costing (C&F vs Transport)
    ---------------------------------------------------------------------
    SELECT 
        'C&F' AS Category,
        ISNULL(SUM(TotalCNFBilling), 0) AS Billing,
        ISNULL(SUM(CNFCosting), 0) AS Costing,
        ISNULL(SUM(CNFGP), 0) AS Profit
    FROM #GPRows
    UNION ALL
    SELECT 
        'Transport' AS Category,
        ISNULL(SUM(BillingTransportBill), 0) AS Billing,
        ISNULL(SUM(CostingTransport), 0) AS Costing,
        ISNULL(SUM(TransportGP), 0) AS Profit
    FROM #GPRows;

    ---------------------------------------------------------------------
    -- RESULT SET 5: Monthly Revenue, Cost & GP%
    ---------------------------------------------------------------------
    SELECT 
        FORMAT(BillDate, 'yyyy-MM') AS MonthKey,
        FORMAT(BillDate, 'MMM yy') AS MonthLabel,
        ISNULL(SUM(TotalBilling), 0) AS Revenue,
        ISNULL(SUM(TotalCosting), 0) AS Cost,
        ISNULL(SUM(TotalGP), 0) AS Profit,
        CASE WHEN SUM(TotalBilling) > 0 THEN ROUND(SUM(TotalGP) * 100.0 / SUM(TotalBilling), 2) ELSE 0 END AS GPMargin
    FROM #GPRows
    GROUP BY FORMAT(BillDate, 'yyyy-MM'), FORMAT(BillDate, 'MMM yy')
    ORDER BY MonthKey ASC;

    ---------------------------------------------------------------------
    -- RESULT SET 6: Top Customers by Gross Profit
    ---------------------------------------------------------------------
    SELECT TOP 7
        CustomerName,
        ISNULL(SUM(TotalGP), 0) AS GrossProfit
    FROM #GPRows
    GROUP BY CustomerName
    ORDER BY GrossProfit DESC;

    ---------------------------------------------------------------------
    -- RESULT SET 7: Detail Grid
    ---------------------------------------------------------------------
    SELECT 
        ROW_NUMBER() OVER (ORDER BY BillDate DESC, BillNo DESC) AS SN,
        CONVERT(VARCHAR(10), BillDate, 103) AS BillingDate,
        BillNo,
        JobNo,
        CustomerName,
        ExpenseType,
        Commission,
        OthersBill,
        BillingTransportBill AS TransportBill,
        TotalBilling AS TotalBill,
        CNFCosting AS CNFCost,
        CostingTransport AS TransCost,
        TotalCosting AS TotalCost,
        CNFGP,
        TransportGP AS TransGP,
        TotalGP,
        TotalGPPercent AS GPPercent
    FROM #GPRows
    ORDER BY BillDate DESC, BillNo DESC;

    DROP TABLE #Bills;
    DROP TABLE #Chc;
    DROP TABLE #Exp;
    DROP TABLE #Cost;
    DROP TABLE #GPRows;
END;
GO
