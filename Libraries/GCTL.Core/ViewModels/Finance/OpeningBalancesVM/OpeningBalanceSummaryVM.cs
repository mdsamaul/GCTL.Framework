namespace GCTL.Core.ViewModels.Finance.OpeningBalancesVM
{
    public class OpeningBalanceSummaryVM
    {
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal Difference => Math.Abs(TotalDebit - TotalCredit);
        public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.005m;
        public string BalanceStatus => IsBalanced ? "Balanced" : (TotalDebit > TotalCredit ? "Dr excess" : "Cr excess");
    }
}
