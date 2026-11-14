using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Dashboard
{
    public class DashboardCashBankKPIs
    {
        public decimal CashInHand { get; set; }
        public decimal BankBalance { get; set; }
        public decimal TrialBalanceTotal { get; set; }
        public bool IsTrialBalanced { get; set; }
        public decimal TrialBalanceDiff { get; set; }
        public decimal NetProfit { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal CurrentAssets { get; set; }
        public decimal CurrentLiabilities { get; set; }
        public decimal CurrentRatio { get; set; }
        public decimal TradeReceivable { get; set; }
        public decimal TradePayable { get; set; }
        public decimal WorkingCapital { get; set; }
        public decimal QuickRatio { get; set; }
        public decimal NetProfitMargin { get; set; }
        public decimal TotalReceipts { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal NetCashBankFlow { get; set; }
    }

    public class DashboardCashBankComposition
    {
        public string Label { get; set; } = string.Empty;
        public string AccType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class DashboardCashBankMonthlyTrend
    {
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public decimal CashAmount { get; set; }
        public decimal BankAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class DashboardCashBankIncomeExpenseTrend
    {
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Expense { get; set; }
        public decimal NetProfit { get; set; }
    }

    public class DashboardCashBankRow
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

    public class DashboardCashBankSummaryDTO
    {
        public DashboardCashBankKPIs KPIs { get; set; } = new();
        public List<DashboardCashBankComposition> Compositions { get; set; } = new();
        public List<DashboardCashBankMonthlyTrend> CashBankTrends { get; set; } = new();
        public List<DashboardCashBankIncomeExpenseTrend> IncomeExpenseTrends { get; set; } = new();
        public List<DashboardCashBankRow> AccountsList { get; set; } = new();
    }
}
