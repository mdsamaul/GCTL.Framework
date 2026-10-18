CREATE PROC [dbo].[Rpt_proc_TrialBalanceReportF2]
    @Main_CompanyCode nvarchar(50),
    @BranchCode       nvarchar(50),
    @FromDate         datetime,
    @ToDate           datetime
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From date = CAST(@FromDate AS date),
            @To   date = CAST(@ToDate   AS date);

    ;WITH AccMap AS (
        SELECT assl.SubSusidiaryLedgerCodeNo,
               assl.TrType,
               gl.GeneralLedgerCodeNo,
               gl.GeneralLedgerName,
               acl.ControlLedgerCodeNo
        FROM Acc_SubSubsidiaryLedger assl
        JOIN Acc_SubsidiaryLedger  asl  ON assl.SubsidiaryLedgerCodeNo  = asl.SusidiaryLedgerCodeNo
        JOIN Acc_GeneralLedger     gl   ON asl.GeneralLedgerCodeNo      = gl.GeneralLedgerCodeNo
        JOIN Acc_SubControlLedger  ascl ON gl.SubControlLedgerCodeNo    = ascl.SubControlLedgerCodeNo
        JOIN Acc_ControlLedger     acl  ON ascl.ControlLedgerCodeNo     = acl.ControlLedgerCodeNo
        WHERE acl.ControlLedgerCodeNo IN ('1','2','3','4')
    ),
    OpeningBal AS (
        SELECT m.GeneralLedgerCodeNo,
               o.BranchCode,
               SUM(o.OpeningBalance) AS OpenAmt
        FROM Acc_Company_Opening_Balance o
        JOIN AccMap m ON o.SubSusidiaryLedgerCodeNo = m.SubSusidiaryLedgerCodeNo
        WHERE ( (m.ControlLedgerCodeNo IN ('1','3') AND m.TrType = 'Dr')
             OR (m.ControlLedgerCodeNo IN ('2','4') AND m.TrType = 'Cr') )
          AND (@Main_CompanyCode = '' OR o.Main_CompanyCode = @Main_CompanyCode)
          AND (@BranchCode       = '' OR o.BranchCode       = @BranchCode)
        GROUP BY m.GeneralLedgerCodeNo, o.BranchCode
    ),
    Vch AS (
        SELECT m.GeneralLedgerCodeNo,
               v.BranchCode,
               SUM(CASE WHEN CAST(v.VoucherDate AS date) <  @From AND d.TrType = 'Dr' THEN d.DebitAmount  ELSE 0 END) AS PreDr,
               SUM(CASE WHEN CAST(v.VoucherDate AS date) <  @From AND d.TrType = 'Cr' THEN d.CreditAmount ELSE 0 END) AS PreCr,
               SUM(CASE WHEN CAST(v.VoucherDate AS date) >= @From AND d.TrType = 'Dr' THEN d.DebitAmount  ELSE 0 END) AS PerDr,
               SUM(CASE WHEN CAST(v.VoucherDate AS date) >= @From AND d.TrType = 'Cr' THEN d.CreditAmount ELSE 0 END) AS PerCr
        FROM Acc_VoucherEntryDetails d
        JOIN AccMap m           ON d.AccCode = m.SubSusidiaryLedgerCodeNo
        JOIN Acc_VoucherEntry v ON d.VoucherEntryCodeNo = v.VoucherEntryCodeNo
        WHERE ISNULL(v.IsApproved,'') = 'Y'
          AND CAST(v.VoucherDate AS date) <= @To
          AND (@Main_CompanyCode = '' OR v.Main_CompanyCode = @Main_CompanyCode)
          AND (@BranchCode       = '' OR v.BranchCode       = @BranchCode)
        GROUP BY m.GeneralLedgerCodeNo, v.BranchCode
    ),
    GL AS (
        SELECT DISTINCT GeneralLedgerCodeNo, GeneralLedgerName, ControlLedgerCodeNo
        FROM AccMap
    ),
    Keys AS (   -- GL + Branch combination (opening ba voucher jekhane ache)
        SELECT GeneralLedgerCodeNo, BranchCode FROM OpeningBal
        UNION
        SELECT GeneralLedgerCodeNo, BranchCode FROM Vch
    ),
    Calc AS (
        SELECT k.GeneralLedgerCodeNo,
               g.GeneralLedgerName,
               k.BranchCode,
               CASE WHEN g.ControlLedgerCodeNo IN ('1','3') THEN 'D' ELSE 'C' END AS Nature,
               CASE WHEN g.ControlLedgerCodeNo IN ('1','3') THEN 1   ELSE -1  END AS Sgn,
               ISNULL(o.OpenAmt,0) AS OpenAmt,
               ISNULL(v.PreDr,0)   AS PreDr,
               ISNULL(v.PreCr,0)   AS PreCr,
               ISNULL(v.PerDr,0)   AS PerDr,
               ISNULL(v.PerCr,0)   AS PerCr
        FROM Keys k
        JOIN GL g ON g.GeneralLedgerCodeNo = k.GeneralLedgerCodeNo
        LEFT JOIN OpeningBal o ON o.GeneralLedgerCodeNo = k.GeneralLedgerCodeNo
                              AND ISNULL(o.BranchCode,'') = ISNULL(k.BranchCode,'')
        LEFT JOIN Vch v        ON v.GeneralLedgerCodeNo = k.GeneralLedgerCodeNo
                              AND ISNULL(v.BranchCode,'') = ISNULL(k.BranchCode,'')
    ),
    Bal AS (
        SELECT GeneralLedgerCodeNo, GeneralLedgerName, BranchCode, Nature, PerDr, PerCr,
               OpenAmt + Sgn * (PreDr - PreCr)                     AS OpeningBal,
               OpenAmt + Sgn * ((PreDr - PreCr) + (PerDr - PerCr)) AS ClosingBal
        FROM Calc
    ),
    Final AS (
        SELECT GeneralLedgerCodeNo,
               GeneralLedgerName,
               BranchCode,
               CASE WHEN Nature = 'D' THEN OpeningBal ELSE 0 END AS OpeningDebit,
               CASE WHEN Nature = 'C' THEN OpeningBal ELSE 0 END AS OpeningCredit,
               PerDr AS PeriodDebit,
               PerCr AS PeriodCredit,
               CASE WHEN Nature = 'D' THEN ClosingBal ELSE 0 END AS Debit,
               CASE WHEN Nature = 'C' THEN ClosingBal ELSE 0 END AS Credit
        FROM Bal
        WHERE OpeningBal <> 0 OR ClosingBal <> 0 OR PerDr <> 0 OR PerCr <> 0
    )
    SELECT GeneralLedgerCodeNo,
           GeneralLedgerName,
           BranchCode,
           OpeningDebit,
           OpeningCredit,
           PeriodDebit,
           PeriodCredit,
           Debit,
           Credit,
           SUM(Debit)  OVER () AS DebitBalance,
           SUM(Credit) OVER () AS CreditBalance
    FROM Final
    ORDER BY GeneralLedgerCodeNo, BranchCode;
END
