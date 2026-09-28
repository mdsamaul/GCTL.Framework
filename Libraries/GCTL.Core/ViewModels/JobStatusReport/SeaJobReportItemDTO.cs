using System;

namespace GCTL.Core.ViewModels.JobStatusReport
{
    /// <summary>
    /// Represents a record for Sea Freight Job Status (From Formosa Customer Portal Website - Sea Port: Both for Import & Export)
    /// Contains all 26 columns required for display and grouped export.
    /// </summary>
    public class SeaJobReportItemDTO
    {
        // Grouping & Context
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;

        // 26 Report Columns matching Formosa Sea Port Specification
        public int SlNo { get; set; }
        public string JobNo { get; set; } = string.Empty;
        public string JobDate { get; set; } = string.Empty;
        public string ShipmentMode { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string DocReceivedDate { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoiceValue { get; set; } = string.Empty;
        public string ContainerPkgs { get; set; } = string.Empty;
        public string Shipper { get; set; } = string.Empty;
        public string BLNo { get; set; } = string.Empty;
        public string ETDDate { get; set; } = string.Empty;
        public string FinalIgmRecDt { get; set; } = string.Empty;
        public string ShipArrivedDate { get; set; } = string.Empty;
        public string DischargeDate { get; set; } = string.Empty;
        public string DocsBankSignDate { get; set; } = string.Empty;
        public string BENo { get; set; } = string.Empty;
        public string BEDate { get; set; } = string.Empty;
        public string EstDeliveryDate { get; set; } = string.Empty;
        public string ActualDeliveryDate { get; set; } = string.Empty;
        public string ShipmentStatus { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public string VesselName { get; set; } = string.Empty;
        public string ContainerNo { get; set; } = string.Empty;
        public string FreightForwarder { get; set; } = string.Empty;

        // Currency Information
        public string CurrencySymbol { get; set; } = "$";
        public string CurrencyName { get; set; } = "USD";

        // UI Helpers
        public string StatusClass { get; set; } = "pending";
    }
}
