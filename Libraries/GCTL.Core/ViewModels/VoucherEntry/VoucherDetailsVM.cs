

namespace GCTL.Core.ViewModels.VoucherEntry
{
    public class VoucherDetailsVM : BaseViewModel
    {
        public decimal autoId { get; set; }
        public string TrType { get; set; } 
        public string AccCode { get; set; } 
        public string AccountHeadName { get; set; } 
        public string Description { get; set; }
        public decimal Amount { get; set; } 
        public string? ChequeNo { get; set; }
        public DateTime ChequeDate { get; set; }
        public string? DebitCreditType { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }

        public decimal? VoucherNo { get; set; }
        public string VoucherEntryDetailsCodeNo { get; set; }
        public string VoucherEntryNo { get; set; }
    }
}
