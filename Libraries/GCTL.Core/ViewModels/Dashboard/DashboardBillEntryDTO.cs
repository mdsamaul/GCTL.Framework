using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Dashboard
{
    public class DashboardBillEntryKPIs
    {
        public int TotalBills { get; set; }
        public decimal NetBillValue { get; set; }
        public decimal TotalOutstandingDue { get; set; }
        public decimal LastServiceValue { get; set; }
        public string LastServiceDate { get; set; } = "-";
        public string LastBillNo { get; set; } = "-";
        public decimal TotalCollected { get; set; }
        public decimal CollectionEfficiency { get; set; }
        public decimal AvgBillValue { get; set; }
        public decimal AgencyCommission { get; set; }
        public decimal TotalDeductions { get; set; }
    }

    public class DashboardBillEntryServiceComp
    {
        public string ServiceHead { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class DashboardBillEntryModeBilling
    {
        public string ModeName { get; set; } = string.Empty;
        public decimal TotalBilling { get; set; }
        public int BillCount { get; set; }
    }

    public class DashboardBillEntryCollectionStatus
    {
        public string StatusName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class DashboardBillEntryMonthlyTrend
    {
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public decimal BilledAmount { get; set; }
        public decimal CollectedAmount { get; set; }
        public decimal DueAmount { get; set; }
    }

    public class DashboardBillEntryTopCustomer
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalBilled { get; set; }
    }

    public class DashboardBillEntryRow
    {
        public long SL { get; set; }
        public string BillNo { get; set; } = string.Empty;
        public string BillDate { get; set; } = string.Empty;
        public string JobNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ExpenseType { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public decimal ReceiptableAmount { get; set; }
        public decimal NonReceiptableAmount { get; set; }
        public decimal TransportAmount { get; set; }
        public decimal AgencyCommission { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Deduction { get; set; }
        public decimal NetAmount { get; set; }
        public decimal DueAmount { get; set; }
        public decimal CollectedAmount { get; set; }
        public string BillStatus { get; set; } = string.Empty;
    }

    public class DashboardBillEntryDTO
    {
        public DashboardBillEntryKPIs KPIs { get; set; } = new();
        public List<DashboardBillEntryServiceComp> ServiceValueCompositions { get; set; } = new();
        public List<DashboardBillEntryModeBilling> ModeBillings { get; set; } = new();
        public List<DashboardBillEntryCollectionStatus> CollectionStatuses { get; set; } = new();
        public List<DashboardBillEntryMonthlyTrend> MonthlyTrends { get; set; } = new();
        public List<DashboardBillEntryTopCustomer> TopCustomers { get; set; } = new();
        public List<DashboardBillEntryRow> BillList { get; set; } = new();
    }
}
