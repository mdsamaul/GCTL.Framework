using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Dashboard
{
    public class DashboardOverviewKPIs
    {
        public int TotalJobs { get; set; }
        public decimal NetServiceValue { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal TotalDueOutstanding { get; set; }
        public decimal CashInHand { get; set; }
        public decimal BankBalance { get; set; }
    }

    public class DashboardOverviewMonthlyTrend
    {
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public decimal GPMargin { get; set; }
    }

    public class DashboardOverviewStatusShare
    {
        public string StatusName { get; set; } = string.Empty;
        public int JobCount { get; set; }
    }

    public class DashboardOverviewTopCustomer
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalBillAmount { get; set; }
    }

    public class DashboardOverviewAgeingBrackets
    {
        public decimal Bracket_0_30 { get; set; }
        public decimal Bracket_31_60 { get; set; }
        public decimal Bracket_61_90 { get; set; }
        public decimal Bracket_90_Plus { get; set; }
    }

    public class DashboardOverviewReqBreakdown
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class DashboardOverviewOverdueBill
    {
        public string BillNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string BillDate { get; set; } = string.Empty;
        public decimal DueAmount { get; set; }
        public int DaysOverdue { get; set; }
    }

    public class DashboardOverviewUpcomingArrival
    {
        public string JobNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ShipmentMode { get; set; } = string.Empty;
        public string ETA { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;
        public string VesselName { get; set; } = string.Empty;
    }

    public class DashboardOverviewDTO
    {
        [System.Text.Json.Serialization.JsonPropertyName("kpis")]
        public DashboardOverviewKPIs KPIs { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("monthlyTrends")]
        public List<DashboardOverviewMonthlyTrend> MonthlyTrends { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("statusShares")]
        public List<DashboardOverviewStatusShare> StatusShares { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("topCustomers")]
        public List<DashboardOverviewTopCustomer> TopCustomers { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("ageingBrackets")]
        public DashboardOverviewAgeingBrackets AgeingBrackets { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("requisitionBreakdowns")]
        public List<DashboardOverviewReqBreakdown> RequisitionBreakdowns { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("overdueBills")]
        public List<DashboardOverviewOverdueBill> OverdueBills { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("upcomingArrivals")]
        public List<DashboardOverviewUpcomingArrival> UpcomingArrivals { get; set; } = new();
    }
}
