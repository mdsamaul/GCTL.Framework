using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Dashboard
{
    public class DashboardAccountsKPIs
    {
        public decimal CashInHand { get; set; }
        public decimal BankBalance { get; set; }
        public decimal TotalReceipts { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal NetFlow { get; set; }
        public string LargestBank { get; set; } = string.Empty;
        public decimal LargestBankAmt { get; set; }
        public int ActiveAccounts { get; set; }
    }

    public class DashboardAccountsBalanceComp
    {
        public string AccName { get; set; } = string.Empty;
        public string AccType { get; set; } = string.Empty;
        public decimal ClosingBalance { get; set; }
    }

    public class DashboardAccountsMonthlyCashFlow
    {
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Receipts { get; set; }
        public decimal Payments { get; set; }
        public decimal NetFlow { get; set; }
    }

    public class DashboardCashBankAccountRow
    {
        public long SN { get; set; }
        public string AccType { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string AccCode { get; set; } = string.Empty;
        public string AccName { get; set; } = string.Empty;
        public decimal Opening { get; set; }
        public decimal Receipts { get; set; }
        public decimal Payments { get; set; }
        public decimal Closing { get; set; }
    }

    public class DashboardAccountsDTO
    {
        public DashboardAccountsKPIs KPIs { get; set; } = new();
        public List<DashboardAccountsBalanceComp> AccountCompositions { get; set; } = new();
        public List<DashboardAccountsMonthlyCashFlow> MonthlyCashFlows { get; set; } = new();
        public List<DashboardCashBankAccountRow> AccountsList { get; set; } = new();
    }
}
