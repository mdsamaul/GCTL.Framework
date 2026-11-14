using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Dashboard
{
    public class DashboardIncomeStatementKPIs
    {
        public decimal TotalRevenue { get; set; }
        public decimal OperatingRevenue { get; set; }
        public decimal NonOperatingRevenue { get; set; }
        public decimal DirectExpenses { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal IndirectExpenses { get; set; }
        public decimal FinanceCost { get; set; }
        public decimal TaxExpense { get; set; }
        public decimal NetProfit { get; set; }
        public decimal GPMarginPct { get; set; }
        public decimal NPMarginPct { get; set; }
        public decimal DirectPctOfRevenue { get; set; }
    }

    public class DashboardIncomeStatementRowDTO
    {
        public string Label { get; set; } = string.Empty;
        public decimal? Amt { get; set; }
        public decimal? Pct { get; set; }
        public string Kind { get; set; } = "item"; // "head", "item", "sub", "total"
    }

    public class DashboardIncomeStatementMonthlyTrend
    {
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Expense { get; set; }
        public decimal NetProfit { get; set; }
    }

    public class DashboardIncomeStatementDTO
    {
        public DashboardIncomeStatementKPIs KPIs { get; set; } = new();
        public List<DashboardIncomeStatementRowDTO> Rows { get; set; } = new();
        public List<DashboardChartSliceDTO> ExpenseComposition { get; set; } = new();
        public List<DashboardIncomeStatementMonthlyTrend> MonthlyTrends { get; set; } = new();
    }
}

