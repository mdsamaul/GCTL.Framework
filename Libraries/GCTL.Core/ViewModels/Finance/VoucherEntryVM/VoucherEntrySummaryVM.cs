namespace GCTL.Core.ViewModels.Finance.VoucherEntryVM
{
    public class VoucherEntrySummaryVM
    {
        public int TotalVouchers { get; set; }
        public int VouchersToday { get; set; }
        public int VouchersThisMonth { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal CashBalance { get; set; }
        public decimal BankBalance { get; set; }

        public int BrvCount { get; set; }
        public decimal BrvAmount { get; set; }

        public int BpvCount { get; set; }
        public decimal BpvAmount { get; set; }

        public int CrvCount { get; set; }
        public decimal CrvAmount { get; set; }

        public int CpvCount { get; set; }
        public decimal CpvAmount { get; set; }

        public int JvCount { get; set; }
        public decimal JvAmount { get; set; }

        public int CvCount { get; set; }
        public decimal CvAmount { get; set; }
    }
}
