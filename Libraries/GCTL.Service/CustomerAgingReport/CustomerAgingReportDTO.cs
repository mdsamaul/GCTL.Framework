using System.Text.Json.Serialization;
using GCTL.Core.ViewModels.Common;

namespace GCTL.Service.CustomerAgingReport
{
    public class CustomerAgingFilter
    {
        [JsonPropertyName("agingDate")]
        public string? AgingDate { get; set; }

        [JsonPropertyName("customerId")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("reportType")]
        public string? ReportType { get; set; } = "Customer Aging"; // "Customer Aging" (Summary) or "Bill Wise Customer Aging"

        [JsonPropertyName("exportFormat")]
        public string? ExportFormat { get; set; } = "PDF";

        // Pagination & Sorting for DataTable
        [JsonPropertyName("draw")]
        public int? Draw { get; set; }

        [JsonPropertyName("start")]
        public int? Start { get; set; }

        [JsonPropertyName("length")]
        public int? Length { get; set; }

        [JsonPropertyName("searchTerm")]
        public string? SearchTerm { get; set; }

        [JsonPropertyName("sortColumn")]
        public string? SortColumn { get; set; }

        [JsonPropertyName("sortDirection")]
        public string? SortDirection { get; set; }
    }

    public class CustomerAgingReportItemDTO
    {
        public int SlNo { get; set; }
        public string CustomerNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? BillNo { get; set; }
        public decimal Ag_0_30 { get; set; }
        public decimal Ag_31_60 { get; set; }
        public decimal Ag_61_90 { get; set; }
        public decimal Ag_91_120 { get; set; }
        public decimal Ag_121_150 { get; set; }
        public decimal Ag_151_180 { get; set; }
        public decimal Ag_181_210 { get; set; }
        public decimal Ag_211_240 { get; set; }
        public decimal Ag_241_270 { get; set; }
        public decimal Ag_271_300 { get; set; }
        public decimal Ag_301_330 { get; set; }
        public decimal Ag_331_360 { get; set; }
        public decimal Ag_Over360 { get; set; }
        public decimal Total { get; set; }
        public string NameofDate { get; set; } = string.Empty;
    }

    public class CustomerAgingSummaryDTO
    {
        public int TotalCustomers { get; set; }
        public int TotalBills { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal Total_0_30 { get; set; }
        public decimal Total_31_90 { get; set; }
        public decimal Total_91_180 { get; set; }
        public decimal Total_Over180 { get; set; }
    }

    public class CustomerAgingGridResult
    {
        [JsonPropertyName("draw")]
        public int Draw { get; set; }

        [JsonPropertyName("recordsTotal")]
        public int RecordsTotal { get; set; }

        [JsonPropertyName("recordsFiltered")]
        public int RecordsFiltered { get; set; }

        [JsonPropertyName("data")]
        public List<CustomerAgingReportItemDTO> Data { get; set; } = new();

        [JsonPropertyName("summary")]
        public CustomerAgingSummaryDTO Summary { get; set; } = new();
    }

    public class CustomerAgingFilterOptionsDTO
    {
        public List<CommonSelectModel> Customers { get; set; } = new();
        public List<string> ReportTypes { get; set; } = new()
        {
            "Customer Aging",
            "Bill Wise Customer Aging"
        };
        public List<string> ExportFormats { get; set; } = new()
        {
            "Preview",
            "PDF",
            "Excel"
        };
    }
}

