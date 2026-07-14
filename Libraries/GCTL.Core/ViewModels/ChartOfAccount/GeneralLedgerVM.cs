namespace GCTL.Core.ViewModels.ChartOfAccount
{
    public class GeneralLedgerVM
    {
        public decimal? autoId { get; set; }

        public string SubsidiaryLedgerCodeNo { get; set; }

        public string SubSusidiaryLedgerCodeNo { get; set; }

        public string SubSubsidiaryLedgerName { get; set; }

        public string? ShortName { get; set; }

        public decimal? OpeningBalance { get; set; }

        public string? TrType { get; set; }

        public DateTime? OpeningDate { get; set; }

        public string? IsActive { get; set; }

        public string? LUser { get; set; }

        public DateTime? LDate { get; set; }

        public string? LIP { get; set; }

        public string? LMAC { get; set; }

        public DateTime? ModifyDate { get; set; }

        public string? CashFlowTypeID { get; set; }

        public string? CostCenterCodeNo { get; set; }

        //Group Ledger
        public string? GeneralgrlName { get; set; }
        public string? Generalgrlcode { get; set; }
        public string? GeneralgrlshortName { get; set; }
        //Control Ledger
        public string? GeneralCRLName { get; set; }
        public string? GeneralCRLCode { get; set; }
        public string? GeneralCRLShortName { get; set; }
        //Sub-Control Ledger
        public string? GeneralSCRLName { get; set; }
        public string? GeneralSCRLCode { get; set; }
        public string? GeneralSCRLShortname { get; set; }
        //Sub-Sidiary Ledger
        public string? GeneralSSLName { get; set; }
        public string? GeneralSSLShortName { get; set; }

        //Cash Flow Type
        public string? CashFlowTypeName { get; set; }

    }
}
