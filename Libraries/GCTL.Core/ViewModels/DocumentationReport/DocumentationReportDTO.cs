using System;

namespace GCTL.Core.ViewModels.DocumentationReport
{
    public class DocumentationReportItemDTO
    {
        public int SlNo { get; set; }
        public string JobNo { get; set; } = string.Empty;
        public string LCANo { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoiceValue { get; set; } = string.Empty;
        public decimal? NumericInvoiceValue { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string ContainerPackages { get; set; } = string.Empty;
        public string Shipper { get; set; } = string.Empty;
        public string BLNo { get; set; } = string.Empty;
        public string LoadingPort { get; set; } = string.Empty;
        public string ETDDate { get; set; } = string.Empty;
        public string ETACTGPORT { get; set; } = string.Empty;
        public string SHIPARRIVEDDATE { get; set; } = string.Empty;
        public string DischargeDate { get; set; } = string.Empty;
        public string ShedYard { get; set; } = string.Empty;
        public string DocumentBankSign { get; set; } = string.Empty;
        public string ETADelivery { get; set; } = string.Empty;
        public string ActualDelivery { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public string VesselRotNo { get; set; } = string.Empty;
        public string ContainerNo { get; set; } = string.Empty;
        public string ForwarderName { get; set; } = string.Empty;
        public string SurrenderedBLIssue { get; set; } = string.Empty;

        // Additional Context & Header
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Address1 { get; set; } = string.Empty;
        public string ShipmentMode { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;
        public string JobDate { get; set; } = string.Empty;
        public string ShipmentStatus { get; set; } = string.Empty;
    }

    public class DocumentationReportFilterVM
    {
        public string? ShipmentMode { get; set; }
        public string? PortType { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? CustomerId { get; set; }
        public string? JobStatus { get; set; }
        public string? JobNo { get; set; }
        public string? ReportOption { get; set; } = "Job Report";
        public string? Format { get; set; }
    }
}
