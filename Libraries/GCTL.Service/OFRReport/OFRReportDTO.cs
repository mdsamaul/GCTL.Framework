using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using GCTL.Core.ViewModels.Common;

namespace GCTL.Service.OFRReport
{
    public class OFRReportFilter
    {
        [JsonPropertyName("companyCode")]
        public string? CompanyCode { get; set; }

        [JsonPropertyName("branchCode")]
        public string? BranchCode { get; set; }

        [JsonPropertyName("fromDate")]
        public string? FromDate { get; set; }

        [JsonPropertyName("toDate")]
        public string? ToDate { get; set; }

        [JsonPropertyName("shipmentModeId")]
        public string? ShipmentModeId { get; set; }

        [JsonPropertyName("customerId")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("jobNo")]
        public string? JobNo { get; set; }

        [JsonPropertyName("ofrReportType")]
        public string? OFRReportType { get; set; } = "OFR Approval Report";

        [JsonPropertyName("serviceProvidedById")]
        public string? ServiceProvidedById { get; set; }

        [JsonPropertyName("exportFormat")]
        public string? ExportFormat { get; set; } = "PDF";

        // Pagination & Sorting (for DataTable grid if applicable)
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

        [JsonPropertyName("sortOrder")]
        public string? SortOrder { get; set; }
    }

    public class OFRReportFilterOptionsDTO
    {
        public List<CommonSelectModel> Companies { get; set; } = new();
        public List<CommonSelectModel> Branches { get; set; } = new();
        public List<CommonSelectModel> ShipmentModes { get; set; } = new();
        public List<CommonSelectModel> Customers { get; set; } = new();
        public List<CommonSelectModel> ServiceProviders { get; set; } = new();
        public List<string> ReportTypes { get; set; } = new()
        {
            "OFR Report",
            "OFR Approval Report",
            "OFR Final Report"
        };
    }

    public class OFRExpenseItemDTO
    {
        public int SlNo { get; set; }
        public string? ExpenseHeadId { get; set; }
        public string ExpenseHead { get; set; } = string.Empty;
        public decimal RequisitionAmount { get; set; }
        public decimal ApprovalAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public bool IsReceiptable { get; set; }
        public string? ServiceTypeId { get; set; }
        public string? ServiceTypeName { get; set; }
        public string? Remarks { get; set; }
    }

    public class OFRServiceGroupDTO
    {
        public string ServiceTypeName { get; set; } = string.Empty;
        public List<OFRExpenseItemDTO> ReceiptableExpenses { get; set; } = new();
        public decimal ReceiptableSubTotalReq { get; set; }
        public decimal ReceiptableSubTotalApp { get; set; }

        public List<OFRExpenseItemDTO> NonReceiptableExpenses { get; set; } = new();
        public decimal NonReceiptableSubTotalReq { get; set; }
        public decimal NonReceiptableSubTotalApp { get; set; }
    }

    public class OFRReportMasterDTO
    {
        public string OfrNo { get; set; } = string.Empty;
        public string JobNo { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string LCValue { get; set; } = "0.00";
        public string BLNo { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string ShipmentMode { get; set; } = string.Empty;
        public string OFRReportType { get; set; } = "Operational Fund Approval";

        // Dynamic Organization & Branch
        public string OrganizationName { get; set; } = "Formosa Logistics Limited";
        public string BranchName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = "21, Hazaribagh, Dhaka-1209";

        public List<OFRServiceGroupDTO> ServiceGroups { get; set; } = new();

        public decimal TotReceiptableReq { get; set; }
        public decimal TotReceiptableApp { get; set; }

        public decimal TotNonReceiptableReq { get; set; }
        public decimal TotNonReceiptableApp { get; set; }

        public decimal TotTransportReq { get; set; }
        public decimal TotTransportApp { get; set; }

        public decimal GrandTotalReq { get; set; }
        public decimal GrandTotalApp { get; set; }

        public string InWords { get; set; } = string.Empty;

        // Signatures
        public string CustomUser { get; set; } = string.Empty;
        public string JettyWarehouseUser { get; set; } = string.Empty;
        public string TransportUser { get; set; } = string.Empty;

        public string PreparedBy { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
        public string CheckedBy { get; set; } = string.Empty;
        public string CertifiedBy { get; set; } = string.Empty;
        public string ApproveBy { get; set; } = string.Empty;

        public string PrintDateTime { get; set; } = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt");
        public string PrintBy { get; set; } = string.Empty;
    }

    public class OFRReportSummaryDTO
    {
        public int TotalRequisitions { get; set; }
        public decimal TotalRequisitionAmount { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public decimal TotalReceiptableAmount { get; set; }
        public decimal TotalNonReceiptableAmount { get; set; }
    }

    public class OFRReportGridRowDTO
    {
        public int SlNo { get; set; }
        public string OfrDate { get; set; } = string.Empty;
        public string OfrNo { get; set; } = string.Empty;
        public string JobNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ShipmentMode { get; set; } = string.Empty;
        public decimal TotalRequisitionAmount { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public decimal TotalReceiptableAmount { get; set; }
        public decimal TotalNonReceiptableAmount { get; set; }
        public string Status { get; set; } = "Approved";
        public string Action { get; set; } = string.Empty;
    }

    public class OFRReportGridResult
    {
        public int TotalCount { get; set; }
        public int FilteredCount { get; set; }
        public List<OFRReportGridRowDTO> Data { get; set; } = new();
    }
}

