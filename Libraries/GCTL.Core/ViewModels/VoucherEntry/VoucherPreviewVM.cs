namespace GCTL.Core.ViewModels.VoucherEntry
{
    public class VoucherPreviewVM
    {
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string VoucherNo { get; set; }
        public DateTime VoucherDate { get; set; }
        public string Narration { get; set; }
        public string AccCode { get; set; }
        public string SubSubsidiaryLedgerName { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public string VoucherTypeName { get; set; } // new
        public string PreparedBy { get; set; }
        public string ReceivedBy { get; set; }
        public string CheckedBy { get; set; }
        public string VerifiedBy { get; set; }
        public string AuthorisedSignature { get; set; }


    }
}
