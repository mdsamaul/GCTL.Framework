using System;
using System.Collections.Generic;
using GCTL.Core.ViewModels;

namespace GCTL.Core.ViewModels.Finance.TrialBalance
{
    public class TrialBalanceFilterVM
    {
        public string? Main_CompanyCode { get; set; } = string.Empty;
        public string? BranchCode { get; set; } = "All";
        public DateTime? FromDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime? ToDate { get; set; } = DateTime.Now;
        public int Level { get; set; } = 5; // 1 to 5
    }

    public class TrialBalanceSummaryVM
    {
        public decimal TotalClosingDebit { get; set; }
        public decimal TotalClosingCredit { get; set; }
        public decimal TotalPeriodDebit { get; set; }
        public decimal TotalPeriodCredit { get; set; }
        public decimal TotalOpeningDebit { get; set; }
        public decimal TotalOpeningCredit { get; set; }
        public decimal Difference { get; set; }
        public bool IsBalanced => Math.Abs(Difference) < 0.01m;
    }

    public class TrialBalanceReportVM
    {
        public TrialBalanceFilterVM Filter { get; set; } = new TrialBalanceFilterVM();
        public TrialBalanceSummaryVM Summary { get; set; } = new TrialBalanceSummaryVM();
        public List<TrialBalanceHierarchyItemDto> Items { get; set; } = new List<TrialBalanceHierarchyItemDto>();
        public List<TrialBalanceSpResultDto> RawItems { get; set; } = new List<TrialBalanceSpResultDto>();
        public List<CommonSelectVM> Companies { get; set; } = new List<CommonSelectVM>();
        public List<CommonSelectVM> Branches { get; set; } = new List<CommonSelectVM>();
        public string CompanyName { get; set; } = "Greenfield International School";
    }
}
