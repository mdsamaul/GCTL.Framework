using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Finance.BalanceSheet
{
    public class BalanceSheetReportVM
    {
        public string CompanyName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromDateFormatted => FromDate.ToString("dd/MM/yyyy");
        public string ToDateFormatted => ToDate.ToString("dd/MM/yyyy");

        // KPI Summary Cards
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalEquity { get; set; }
        public decimal CurrentPeriodSurplus { get; set; }
        public decimal BalanceDifference => Math.Round(TotalAssets - (TotalLiabilities + TotalEquity), 2);
        public bool IsBalanced => Math.Abs(BalanceDifference) < 0.005m;

        // Formatted KPI Values
        public string TotalAssetsFormatted => "৳ " + TotalAssets.ToString("N2");
        public string TotalLiabilitiesFormatted => "৳ " + TotalLiabilities.ToString("N2");
        public string TotalEquityFormatted => "৳ " + TotalEquity.ToString("N2");
        public string BalanceCheckLabel => IsBalanced ? "✔ Balanced" : "✖ Difference";
        public string BalanceCheckSub => IsBalanced ? "Assets = Equities & Liabilities" : "৳ " + Math.Abs(BalanceDifference).ToString("N2");
        public string BalanceCheckClass => IsBalanced ? "ok" : "bad";

        // Side-by-side collections
        public List<BalanceSheetItemVM> AssetsItems { get; set; } = new List<BalanceSheetItemVM>();
        public List<BalanceSheetItemVM> EquitiesAndLiabilitiesItems { get; set; } = new List<BalanceSheetItemVM>();

        public decimal TotalAssetsAmount { get; set; }
        public decimal TotalEquitiesAndLiabilitiesAmount { get; set; }
        public string TotalAssetsAmountFormatted => "৳ " + TotalAssetsAmount.ToString("N2");
        public string TotalEquitiesAndLiabilitiesAmountFormatted => "৳ " + TotalEquitiesAndLiabilitiesAmount.ToString("N2");
    }
}
