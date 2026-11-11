using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Dashboard
{
    public class DashboardJobStatusKPIs
    {
        public int TotalJobs { get; set; }
        public int PendingJobs { get; set; }
        public int InTransitJobs { get; set; }
        public int DeliveredJobs { get; set; }
        public decimal TotalLCValue { get; set; }
        public decimal TotalGrossWeight { get; set; }
        public decimal DeliveryRate { get; set; }
    }

    public class DashboardJobStatusShare
    {
        public string StatusName { get; set; } = string.Empty;
        public int JobCount { get; set; }
    }

    public class DashboardJobStatusMode
    {
        public string ModeName { get; set; } = string.Empty;
        public int JobCount { get; set; }
    }

    public class DashboardJobStatusPort
    {
        public string PortName { get; set; } = string.Empty;
        public int JobCount { get; set; }
    }

    public class DashboardJobStatusMonthlyTrend
    {
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public int TotalJobs { get; set; }
        public int DeliveredJobs { get; set; }
    }

    public class DashboardJobStatusTopCustomer
    {
        public string CustomerName { get; set; } = string.Empty;
        public int TotalJobs { get; set; }
    }

    public class DashboardJobStatusRow
    {
        public long SN { get; set; }
        public string JobNo { get; set; } = string.Empty;
        public string JobDate { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ExpenseType { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;
        public string BENo { get; set; } = string.Empty;
        public string BEDate { get; set; } = string.Empty;
        public string ShipmentStatus { get; set; } = string.Empty;
        public decimal LCValue { get; set; }
        public decimal GrossWeight { get; set; }
    }

    public class DashboardJobStatusDTO
    {
        public DashboardJobStatusKPIs KPIs { get; set; } = new();
        public List<DashboardJobStatusShare> StatusShares { get; set; } = new();
        public List<DashboardJobStatusMode> ModeDistributions { get; set; } = new();
        public List<DashboardJobStatusPort> PortWorkloads { get; set; } = new();
        public List<DashboardJobStatusMonthlyTrend> MonthlyTrends { get; set; } = new();
        public List<DashboardJobStatusTopCustomer> TopCustomers { get; set; } = new();
        public List<DashboardJobStatusRow> JobList { get; set; } = new();
    }
}
