

namespace GCTL.Core.ViewModels.ChartOfAccount
{
    public class SubControlLedgerVM
    {
        public decimal? autoId { get; set; }

        public string SubControlLedgerCodeNo { get; set; }

        public string GeneralLedgerCodeNo { get; set; }

        public string GeneralLedgerName { get; set; }

        public string? ShortName { get; set; }

        //public string IsSameNameGL { get; set; }

        //public string IsSameNameSL { get; set; }

        public string? LUser { get; set; }

        public DateTime? LDate { get; set; }

        public string? LIP { get; set; }

        public string? LMAC { get; set; }

        public DateTime? ModifyDate { get; set; }
        
        //Control Ledger
        public string? CRLShortname { get; set; }
        //public string? CRLCode { get; set; }

        //Group Ledger
        public string? GRLCode { get; set; }
        public string? GRLShortname { get; set; }

    }
}
