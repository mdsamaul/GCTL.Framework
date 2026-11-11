using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Dashboard
{
    public class DashboardJobReconcileKPIs
    {
        public int TotalBills { get; set; }
        public decimal NetBillValue { get; set; }
        public decimal TotalCosting { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GPMargin { get; set; }
        public decimal AvgProfitPerJob { get; set; }
        public decimal AvgExpensePerJob { get; set; }
        public string BestCustomer { get; set; } = string.Empty;
        public decimal BestCustomerGP { get; set; }
        public string LowestMarginJob { get; set; } = string.Empty;
        public decimal LowestMarginGP { get; set; }
        public string LargestExpenseHead { get; set; } = string.Empty;
        public decimal LargestExpenseHeadAmt { get; set; }
    }

    public class DashboardJobReconcileExpenseHead
    {
        public string ExpenseHead { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class DashboardJobReconcileGPBracket
    {
        public int Bracket_Neg { get; set; }
        public int Bracket_0_20 { get; set; }
        public int Bracket_20_30 { get; set; }
        public int Bracket_30_40 { get; set; }
        public int Bracket_40_50 { get; set; }
        public int Bracket_50_Plus { get; set; }
    }

    public class DashboardJobReconcileCustomer
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Expense { get; set; }
        public decimal Profit { get; set; }
    }

    public class DashboardJobReconcileRow
    {
        public long SN { get; set; }
        public string JobNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Head1Amt { get; set; }
        public string Head1Name { get; set; } = string.Empty;
        public decimal Head2Amt { get; set; }
        public string Head2Name { get; set; } = string.Empty;
        public decimal Head3Amt { get; set; }
        public string Head3Name { get; set; } = string.Empty;
        public decimal OtherExpenses { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal Profit { get; set; }
        public decimal GPPercent { get; set; }
    }

    public class DashboardJobReconcileDTO
    {
        public DashboardJobReconcileKPIs KPIs { get; set; } = new();
        public List<DashboardJobReconcileExpenseHead> TopExpenseHeads { get; set; } = new();
        public DashboardJobReconcileGPBracket GPBrackets { get; set; } = new();
        public List<DashboardJobReconcileCustomer> TopCustomers { get; set; } = new();
        public List<DashboardJobReconcileRow> ReconcileList { get; set; } = new();
    }
}
