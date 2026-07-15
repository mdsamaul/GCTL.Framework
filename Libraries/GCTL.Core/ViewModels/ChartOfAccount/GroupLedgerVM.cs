

namespace GCTL.Core.ViewModels.ChartOfAccount
{
    public class GroupLedgerVM
    {
        public decimal? autoId { get; set; }

        public string ControlLedgerCodeNo { get; set; }

        public string ControlLedgerName { get; set; }

        public string? ShortName { get; set; }

        public string? LUser { get; set; }

        public DateTime? LDate { get; set; }

        public string? LIP { get; set; }

        public string? LMAC { get; set; }

        public DateTime? ModifyDate { get; set; }
    }
}
