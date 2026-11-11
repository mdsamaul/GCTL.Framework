using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Dashboard
{
    public class DashboardGPAnalysisKPIs
    {
        public int TotalBills { get; set; }
        public decimal NetBillValue { get; set; }
        public decimal TotalCosting { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GPMargin { get; set; }
        public decimal CNFProfit { get; set; }
        public decimal TransportProfit { get; set; }
        public decimal AirExportProfit { get; set; }
        public decimal AirExportGPPercent { get; set; }
        public string BestMode { get; set; } = string.Empty;
        public decimal BestModeGP { get; set; }
        public string LowestMode { get; set; } = string.Empty;
        public decimal LowestModeGP { get; set; }
        public decimal AvgProfitPerBill { get; set; }
    }

    public class DashboardGPCostComposition
    {
        public string CostHead { get; set; } = string.Empty;
        public decimal CostAmount { get; set; }
    }

    public class DashboardGPModeMargin
    {
        public string ModeName { get; set; } = string.Empty;
        public decimal TotalBilling { get; set; }
        public decimal TotalCosting { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal GPPercent { get; set; }
    }

    public class DashboardGPBillingVsCosting
    {
        public string Category { get; set; } = string.Empty;
        public decimal Billing { get; set; }
        public decimal Costing { get; set; }
        public decimal Profit { get; set; }
    }

    public class DashboardGPMonthlyTrend
    {
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
        public decimal GPMargin { get; set; }
    }

    public class DashboardGPTopCustomer
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal GrossProfit { get; set; }
    }

    public class DashboardGPRow
    {
        public long SN { get; set; }
        public string BillingDate { get; set; } = string.Empty;
        public string BillNo { get; set; } = string.Empty;
        public string JobNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ExpenseType { get; set; } = string.Empty;
        public decimal Commission { get; set; }
        public decimal OthersBill { get; set; }
        public decimal TransportBill { get; set; }
        public decimal TotalBill { get; set; }
        public decimal CNFCost { get; set; }
        public decimal TransCost { get; set; }
        public decimal TotalCost { get; set; }
        public decimal CNFGP { get; set; }
        public decimal TransGP { get; set; }
        public decimal TotalGP { get; set; }
        public decimal GPPercent { get; set; }
    }

    public class DashboardGPAnalysisDTO
    {
        public DashboardGPAnalysisKPIs KPIs { get; set; } = new();
        public List<DashboardGPCostComposition> CostCompositions { get; set; } = new();
        public List<DashboardGPModeMargin> ModeMargins { get; set; } = new();
        public List<DashboardGPBillingVsCosting> BillingVsCostings { get; set; } = new();
        public List<DashboardGPMonthlyTrend> MonthlyTrends { get; set; } = new();
        public List<DashboardGPTopCustomer> TopCustomers { get; set; } = new();
        public List<DashboardGPRow> GPList { get; set; } = new();
    }
}
