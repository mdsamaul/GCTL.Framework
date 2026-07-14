
namespace GCTL.Core.ViewModels.ChartOfAccount
{
    public class ControlLedgerVM
    {
        public decimal? autoId { get; set; }

        public string ControlLedgerCodeNo { get; set; }

        public string SubControlLedgerCodeNo { get; set; }

        public string SubControlLedgerName { get; set; }

        public string? ShortName { get; set; }

        public string? LUser { get; set; }

        public DateTime? LDate { get; set; }

        public string? LIP { get; set; }

        public string? LMAC { get; set; }

        public DateTime? ModifyDate { get; set; }

        public string? GroupLedgerName { get; set; }
        public string? GroupLedgerShortName { get; set; }
    }
}
