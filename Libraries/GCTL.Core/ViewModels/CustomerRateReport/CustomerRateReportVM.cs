using System;
using System.Collections.Generic;
using GCTL.Core.ViewModels.Common;

namespace GCTL.Core.ViewModels.CustomerRateReport
{
    public class CustomerRateReportFilterVM : BaseViewModel
    {
        public string? CustomerId { get; set; }
        public string? CustomerIds { get; set; }

        // DataTables pagination & sorting params
        public int? Draw { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? Start { get; set; }
        public int? Length { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortColumn { get; set; }
        public string? SortOrder { get; set; }
    }

    public class CustomerRateReportItemDTO
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Particulars { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string FormattedAmount { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public bool IsHeader { get; set; }
        public bool IsCustomerHeader { get; set; }
    }

    public class CustomerRateReportSummaryDTO
    {
        public int TotalCustomers { get; set; }
        public int TotalChcItems { get; set; }
        public decimal TotalHandlingAmount { get; set; }
        public decimal MinAgencyCommission { get; set; }
        public decimal OnInvoiceValue { get; set; }
    }

    public class CustomerRateReportPdfModel
    {
        public string CompanyName { get; set; } = "Formosa Logistics Limited";
        public string CompanyAddress { get; set; } = "9th Floor, TCB Bhaban, 1 Kawran Bazar\nRd, Dhaka 1215";
        public string ReportTitle { get; set; } = "Customer Rate Report";
        public string PrintDate { get; set; } = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt");

        public List<CustomerRateReportCustomerGroupPdfModel> CustomerGroups { get; set; } = new();
    }

    public class CustomerRateReportCustomerGroupPdfModel
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public decimal TotalHandlingAmount { get; set; }
        public decimal MinAgencyCommission { get; set; }
        public decimal OnInvoiceValue { get; set; }
        public List<CustomerRateReportItemPdfModel> Items { get; set; } = new();
    }

    public class CustomerRateReportItemPdfModel
    {
        public string Section { get; set; } = string.Empty; // "A.", "B.", or ""
        public string Particulars { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public bool IsSectionHeader { get; set; }
    }
}
