using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.JobWiseReconcileStatement
{
    public class JobWiseReconcileStatementFilterVM
    {
        public string? ExpenseTypeId { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? CustomerId { get; set; }
        public string? JobNo { get; set; }
        public string ExportFormat { get; set; } = "XLS"; // "XLS" or "XLSX"

        // DataTables pagination parameters
        public int? Draw { get; set; }
        public int? Start { get; set; }
        public int? Length { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class JobWiseReconcileReportResultVM
    {
        public List<string> Headers { get; set; } = new();
        public List<string> ExpenseHeadColumns { get; set; } = new();
        public List<Dictionary<string, object>> Rows { get; set; } = new();
        public JobWiseReconcileSummaryVM Summary { get; set; } = new();
    }

    public class JobWiseReconcileSummaryVM
    {
        public int TotalJobs { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetProfit { get; set; }
        public Dictionary<string, decimal> ColumnTotals { get; set; } = new();
    }

    public class JobWiseReconcileFilterOptionsVM
    {
        public List<GCTL.Core.ViewModels.Common.CommonSelectModel> ShipmentModes { get; set; } = new();
        public List<GCTL.Core.ViewModels.Common.CommonSelectModel> Customers { get; set; } = new();
        public List<string> JobNos { get; set; } = new();
    }
}
