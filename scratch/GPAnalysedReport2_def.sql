CREATE PROC [dbo].[GPAnalysedReport2]
    @ShipmentMode nvarchar(50),
    @CustomerID   nvarchar(50),
    @DateUse      nvarchar(10),
    @FromDate     date,
    @ToDate       date
AS
SET NOCOUNT ON;

/* 1. Bills in scope (filter FIRST) + commission calculated once */
SELECT  be.BillNo, be.JobNo, be.BillDate,
        d.CustomerID, c.CustomerName,
        d.ExpenseTypeID, et.ExpenseType,
        CASE WHEN x.Calc > be.MinimumAgencyCommission THEN x.Calc
             ELSE be.MinimumAgencyCommission END AS Commission
INTO    #Bills
FROM    CF_BillEntry be
JOIN    Sep_Documentation d      ON d.JobNo = be.JobNo
LEFT JOIN CF_Def_ExpenseType et  ON et.ExpenseTypeID = d.ExpenseTypeID
LEFT JOIN Sales_Customer c       ON c.CustomerID = d.CustomerID
CROSS APPLY (SELECT CAST(
        be.OnInvoiceValue
        * CASE WHEN be.LCValue IS NULL OR be.LCValue = 0 THEN d.LCValue ELSE be.LCValue END
        * ISNULL(be.FOBValueBDT, 1) / 100 AS decimal(18,2)) AS Calc) x
WHERE   (@CustomerID   = '' OR d.CustomerID     = @CustomerID)
  AND   (@ShipmentMode = '' OR d.ExpenseTypeID  = @ShipmentMode)
  AND   (@DateUse = 'N' OR (@DateUse = 'Y' AND be.BillDate BETWEEN @FromDate AND @ToDate))
OPTION (RECOMPILE);

CREATE CLUSTERED INDEX IX_Bills_BillNo ON #Bills(BillNo);
CREATE INDEX IX_Bills_JobNo ON #Bills(JobNo);

/* 2. CHC billing: transport + CNF receptable / non-receptable, one pass */
SELECT  ch.BillNo,
        SUM(CASE WHEN ch.CHCID IN ('00009','00021') THEN ch.BillAmount END) AS TransportBill,
        SUM(CASE WHEN ch.CHCID NOT IN ('00009','00021') AND f.Flag = 'Y' THEN ch.BillAmount END) AS Rec,
        SUM(CASE WHEN ch.CHCID NOT IN ('00009','00021') AND f.Flag = 'N' THEN ch.BillAmount END) AS NonRec
INTO    #Chc
FROM    CF_BillEntryDetails_CHCHead ch
JOIN    #Bills b                   ON b.BillNo = ch.BillNo
JOIN    CustomerHandilingCharge h  ON h.CHCID  = ch.CHCID
CROSS APPLY (SELECT CASE WHEN COALESCE(NULLIF(ch.IsReceiptable,''), h.IsReceiptable) = 'Y'
                         THEN 'Y' ELSE 'N' END AS Flag) f
GROUP BY ch.BillNo;

CREATE CLUSTERED INDEX IX_Chc ON #Chc(BillNo);

/* 3. Expense-head billing */
SELECT  bed.BillNo,
        SUM(CASE WHEN eh.IsReceiptable = 'Y' THEN ISNULL(bed.ExpenseBillAmount,0) END) AS Rec,
        SUM(CASE WHEN eh.IsReceiptable = 'N' THEN ISNULL(bed.ExpenseBillAmount,0) END) AS NonRec
INTO    #Exp
FROM    CF_BillEntryDetails bed
JOIN    #Bills b                  ON b.BillNo = bed.BillNo
LEFT JOIN CF_Def_ExpenseHead eh   ON eh.ExpenseHeadID = bed.ExpenseHeadID
GROUP BY bed.BillNo;

CREATE CLUSTERED INDEX IX_Exp ON #Exp(BillNo);

/* 4. Costing: aggregate by JobNo ONLY for jobs in scope, one pass */
SELECT  bd.JobNo,
        SUM(CASE WHEN bd.ServiceTypeID <> '0003' AND beh.IsReceiptable = 'Y' THEN ISNULL(bd.BillAdjustAmount,0) ELSE 0 END) AS CostRec,
        SUM(CASE WHEN bd.ServiceTypeID <> '0003' AND beh.IsReceiptable = 'N' THEN ISNULL(bd.BillAdjustAmount,0) ELSE 0 END) AS CostNonRec,
        SUM(CASE WHEN bd.ServiceTypeID  = '0003'                              THEN ISNULL(bd.BillAdjustAmount,0) ELSE 0 END) AS CostTransport
INTO    #Cost
FROM    CF_OperationalFundRequisitionDetails bd
LEFT JOIN CF_Def_ExpenseHead beh ON beh.ExpenseHeadID = bd.ExpenseHeadID
WHERE   bd.JobNo IN (SELECT JobNo FROM #Bills)
GROUP BY bd.JobNo;

CREATE CLUSTERED INDEX IX_Cost ON #Cost(JobNo);

/* 5. Final result, no window functions, no extra temp tables */
SELECT  b.ExpenseType, b.ExpenseTypeID, b.CustomerName, b.CustomerID, b.JobNo, b.BillNo,
        CONVERT(varchar(10), b.BillDate, 103) AS BillDate,
        a.BillingTransportBill,
        a.BillingReceptableAmount,
        a.BillingNonReceptableAmount,
        b.Commission AS BillingCommission,
        a.CostingReceptableAmount,
        a.CostingNonReceptableAmount,
        a.CostingHairTransportAmount,
        a.CostingSwiftTransportAmount,
        t.OthersBill, t.TotalCNFBilling, t.CNFCosting, t.CostingTransport,
        t.TotalBilling, t.TotalCosting,
        g.TotalGP, g.CNFGP, g.TransportGP,
        CONCAT(CAST(ROUND(CASE WHEN t.TotalBilling    > 0 THEN g.TotalGP     * 100.0 / t.TotalBilling    ELSE 0 END,2) AS numeric(36,2)),'%') AS TotalGPPercent,
        CONCAT(CAST(ROUND(CASE WHEN t.TotalCNFBilling > 0 THEN g.CNFGP       * 100.0 / t.TotalCNFBilling ELSE 0 END,2) AS numeric(36,2)),'%') AS CNFGPPercent,
        CONCAT(CAST(ROUND(CASE WHEN a.BillingTransportBill > 0 THEN g.TransportGP * 100.0 / a.BillingTransportBill ELSE 0 END,2) AS numeric(36,2)),'%') AS TransportGPPercent
FROM    #Bills b
LEFT JOIN #Chc  chc ON chc.BillNo = b.BillNo
LEFT JOIN #Exp  ex  ON ex.BillNo  = b.BillNo
LEFT JOIN #Cost cst ON cst.JobNo  = b.JobNo
CROSS APPLY (SELECT
        ISNULL(chc.TransportBill,0)                      AS BillingTransportBill,
        ISNULL(chc.Rec,0)    + ISNULL(ex.Rec,0)          AS BillingReceptableAmount,
        ISNULL(chc.NonRec,0) + ISNULL(ex.NonRec,0)       AS BillingNonReceptableAmount,
        ISNULL(cst.CostRec,0)                            AS CostingReceptableAmount,
        ISNULL(cst.CostNonRec,0)                         AS CostingNonReceptableAmount,
        ISNULL(cst.CostTransport,0)                      AS CostingHairTransportAmount,
        CAST(0 AS decimal(18,2))                         AS CostingSwiftTransportAmount) a
CROSS APPLY (SELECT
        a.BillingReceptableAmount + a.BillingNonReceptableAmount                         AS OthersBill,
        a.BillingReceptableAmount + a.BillingNonReceptableAmount + b.Commission          AS TotalCNFBilling,
        a.CostingReceptableAmount + a.CostingNonReceptableAmount                         AS CNFCosting,
        a.CostingHairTransportAmount + a.CostingSwiftTransportAmount                     AS CostingTransport) t0
CROSS APPLY (SELECT t0.OthersBill, t0.TotalCNFBilling, t0.CNFCosting, t0.CostingTransport,
        a.BillingTransportBill + t0.TotalCNFBilling AS TotalBilling,
        t0.CNFCosting + t0.CostingTransport         AS TotalCosting) t
CROSS APPLY (SELECT
        t.TotalBilling - t.TotalCosting             AS TotalGP,
        t.TotalCNFBilling - t.CNFCosting            AS CNFGP,
        a.BillingTransportBill - t.CostingTransport AS TransportGP) g;
