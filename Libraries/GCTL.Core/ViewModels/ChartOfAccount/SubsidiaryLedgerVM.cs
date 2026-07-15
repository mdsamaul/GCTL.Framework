

namespace GCTL.Core.ViewModels.ChartOfAccount
{
    public class SubsidiaryLedgerVM
    {
        public decimal? autoId { get; set; }

        public string GeneralLedgerCodeNo { get; set; }

        public string SusidiaryLedgerCodeNo { get; set; }

        public string SubsidiaryLedgerName { get; set; }

        public string? ShortName { get; set; }

        //public string? IsSameNameSL { get; set; }

        public string? LUser { get; set; }

        public DateTime? LDate { get; set; }

        public string? LIP { get; set; }

        public string? LMAC { get; set; }

        public DateTime? ModifyDate { get; set; }

        //Group Ledger
        public string? GLCode { get; set; }
        public string? GLShortName { get; set; }

        //Control Ledger 
        public string? CRLCode { get; set; }
        public string? CRLShortName { get; set; }

        //Sub Control Ledger
        public string? SubCRLShortName { get; set; }


    }
}
