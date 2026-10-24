using System;
using System.Collections.Generic;
using GCTL.Core.ViewModels.Common;

namespace GCTL.Core.ViewModels.RequisitionSummaryReport
{
    public class RequisitionSummaryReportFilterVM : BaseViewModel
    {
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? CustomerIds { get; set; }
        public string? PortIds { get; set; }
        public string? ShipmentModeIds { get; set; }
        public string? JobNos { get; set; }
        public string? SelectedJobNos { get; set; }

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

    public class RequisitionSummaryReportItemDTO
    {
        public int Sl { get; set; }
        public string JobNo { get; set; } = string.Empty;
        public string JobDate { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PortType { get; set; } = string.Empty;
        public string ShipmentMode { get; set; } = string.Empty;
        public decimal Receiptable { get; set; }
        public decimal NonReceiptable { get; set; }
        public decimal Total { get; set; }
    }

    public class RequisitionSummaryReportSummaryDTO
    {
        public int TotalJobs { get; set; }
        public decimal TotalReceiptable { get; set; }
        public decimal TotalNonReceiptable { get; set; }
        public decimal GrandTotal { get; set; }
    }

    public class RequisitionSummaryReportFilterOptionsDTO
    {
        public List<CommonSelectModel> Customers { get; set; } = new();
        public List<CommonSelectModel> PortTypes { get; set; } = new();
        public List<CommonSelectModel> ShipmentModes { get; set; } = new();
        public List<string> JobNos { get; set; } = new();
    }

    public class RequisitionSummaryPdfModel
    {
        public string CompanyName { get; set; } = "Formosa Logistics Limited";
        public string CompanyAddress { get; set; } = "9th Floor, TCB Bhaban, 1 Kawran Bazar\nRd, Dhaka 1215";
        public string ReportTitle { get; set; } = "Requisition Summary Report";
        public string PrintDate { get; set; } = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt");
        public List<RequisitionSummaryCustomerGroupPdfModel> CustomerGroups { get; set; } = new();
        public int TotalJobsCount { get; set; }
        public decimal GrandTotalReceiptable { get; set; }
        public decimal GrandTotalNonReceiptable { get; set; }
        public decimal GrandTotalCost { get; set; }
    }

    public class RequisitionSummaryCustomerGroupPdfModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public List<RequisitionSummaryItemPdfModel> Items { get; set; } = new();
        public decimal SubtotalReceiptable { get; set; }
        public decimal SubtotalNonReceiptable { get; set; }
        public decimal SubtotalTotal { get; set; }
    }

    public class RequisitionSummaryItemPdfModel
    {
        public int SlNo { get; set; }
        public string JobNo { get; set; } = string.Empty;
        public string JobDate { get; set; } = string.Empty;
        public string ShipmentMode { get; set; } = string.Empty;
        public decimal Receiptable { get; set; }
        public decimal NonReceiptable { get; set; }
        public decimal Total { get; set; }
    }
}
