using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Dashboard
{
    public class DashboardTrialBalanceKPIs
    {
        public decimal ClosingDebit { get; set; }
        public decimal ClosingCredit { get; set; }
        public decimal PeriodDebit { get; set; }
        public decimal PeriodCredit { get; set; }
        public bool IsBalanced { get; set; }
        public decimal Difference { get; set; }
    }

    public class DashboardTrialBalanceGroupChart
    {
        public string Code { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public decimal ClosingDebit { get; set; }
        public decimal ClosingCredit { get; set; }
    }

    public class DashboardTrialBalanceControlChart
    {
        public string Code { get; set; } = string.Empty;
        public string ControlName { get; set; } = string.Empty;
        public decimal ClosingDebit { get; set; }
    }

    public class DashboardTrialBalanceRow
    {
        public int Level { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ParticularName { get; set; } = string.Empty;
        public string Particulars { get; set; } = string.Empty;
        public string ParentCode { get; set; } = string.Empty;
        public decimal OpDr { get; set; }
        public decimal OpCr { get; set; }
        public decimal TrDr { get; set; }
        public decimal TrCr { get; set; }
        public decimal ClDr { get; set; }
        public decimal ClCr { get; set; }
    }

    public class DashboardTrialBalanceDTO
    {
        public DashboardTrialBalanceKPIs KPIs { get; set; } = new DashboardTrialBalanceKPIs();
        public List<DashboardTrialBalanceGroupChart> GroupChart { get; set; } = new List<DashboardTrialBalanceGroupChart>();
        public List<DashboardTrialBalanceControlChart> ControlChart { get; set; } = new List<DashboardTrialBalanceControlChart>();
        public List<DashboardTrialBalanceRow> Rows { get; set; } = new List<DashboardTrialBalanceRow>();
    }
}

