namespace GCTL.Core.ViewModels.ExpenseHead
{
    public class ExpenseHeadReportVM
    {
        public string ExpenseHeadID { get; set; } = string.Empty;
        public string ExpenseHead { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string SerialNo { get; set; } = string.Empty;
        public string IsReceiptable { get; set; } = string.Empty;
        public string ServiceProvidedBy { get; set; } = string.Empty;
        public string ExpenseType { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string Status { get; set; } = "Active";
        public string Company { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}
