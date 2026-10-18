using System;

namespace GCTL.Core.ViewModels.Finance.GeneralLedgerReport
{
    public class GeneralLedgerItemVM
    {
        public DateTime? Date { get; set; }
        public string DateFormatted => Date.HasValue ? Date.Value.ToString("dd/MM/yyyy") : string.Empty;
        public string VoucherNo { get; set; } = string.Empty;
        public string? VoucherType { get; set; }
        public string? InvoiceNo { get; set; }
        public string? Narration { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
        public string RunningBalanceFormatted { get; set; } = "0.00";
    }
}
