namespace GCTL.Core.ViewModels.PaymentReceiveEntry
{
    public class PaymentListItemViewModel
    {
        public long RowNumber { get; set; }
        public string PaymentReceiveID { get; set; } = string.Empty;
        public string? PaymentReceiveDate { get; set; }
        public string? Amount { get; set; }
        public string? AgainstBillList { get; set; }
        public string? BankFeeOnlineCharge { get; set; }
        public string? PaymentType { get; set; }
        public string? PaymentModeName { get; set; }
        public string? ChequeNo { get; set; }
        public string? ChequeDate { get; set; }
        public int RecordCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }
}
