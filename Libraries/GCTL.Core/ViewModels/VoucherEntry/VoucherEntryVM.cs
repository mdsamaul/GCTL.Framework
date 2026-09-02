namespace GCTL.Core.ViewModels.VoucherEntry
{
    public class VoucherEntryVM : BaseViewModel
    {
        // Main Information
        public string CompanyCode { get; set; }
        public string BranchCode { get; set; }
        public string VoucherType_Code { get; set; }
        public DateTime VoucherDate { get; set; }
        public string? VoucherDateDisplay { get; set; }
        public string? VoucherEntryCodeNo { get; set; }
        public string Narration { get; set; }
        public string VoucherNo { get; set; }
        public string? BranchName { get; set; }
        public decimal? Amount { get; set; }
        public string? InvoiceNo { get; set; }
        public decimal? autoId { get; set; }



        //Details Information List
        //public List<VoucherDetailsVM> Details { get; set; } = new List<VoucherDetailsVM>();
    }
}
