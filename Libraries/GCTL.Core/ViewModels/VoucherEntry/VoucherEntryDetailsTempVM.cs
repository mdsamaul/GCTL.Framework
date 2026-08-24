namespace GCTL.Core.ViewModels.VoucherEntry
{
    public class VoucherEntryDetailsTempVM : BaseViewModel
    {
        public decimal? autoId { get; set; }

        public string VoucherEntryDetailsCodeNo { get; set; }

        public string? VoucherEntryCodeNo { get; set; }

        public string AccCode { get; set; }

        public string TrType { get; set; }

        public string? Description { get; set; }

        public decimal? DebitAmount { get; set; }

        public decimal? CreditAmount { get; set; }

        public string? ChequeNo { get; set; }

        public DateTime ChequeDate { get; set; }

        public string? LUser { get; set; }

        public string? AccountHead { get; set; }

        //joining data 
        public string? GroupLedgerName { get; set; }
        public string? ControlLedgerName { get; set; }
        public string? SubControlLedgerName { get; set; }
        public string? SubSidiaryLedgerName { get; set; }
    }
}
