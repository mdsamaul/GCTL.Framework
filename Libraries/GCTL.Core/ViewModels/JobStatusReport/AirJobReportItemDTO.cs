using System;

namespace GCTL.Core.ViewModels.JobStatusReport
{
    /// <summary>
    /// Represents a record for Air Freight Job Status (From Formosa Customer Portal Website - Air Port: Both for Import & Export)
    /// Contains all 21 columns required for display and grouped export.
    /// </summary>
    public class AirJobReportItemDTO
    {
        // Grouping & Context
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;

        // 21 Report Columns matching Formosa Air Port Specification
        public int SlNo { get; set; }
        public string JobNo { get; set; } = string.Empty;
        public string JobDate { get; set; } = string.Empty;
        public string ShipmentMode { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string DocReceivedDate { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoiceValue { get; set; } = string.Empty;
        public string MaterialNamePkgsQty { get; set; } = string.Empty;
        public string ExpressNameCountry { get; set; } = string.Empty;
        public string LCNo { get; set; } = string.Empty;
        public string HawbNo { get; set; } = string.Empty;
        public string MawbNo { get; set; } = string.Empty;
        public string ETDDate { get; set; } = string.Empty;
        public string FlightActualDeliveryDate { get; set; } = string.Empty;
        public string ArrivalNoticeRcvdExpressDate { get; set; } = string.Empty;
        public string BENo { get; set; } = string.Empty;
        public string BEDate { get; set; } = string.Empty;
        public string ActualDeliveryDate { get; set; } = string.Empty;
        public string ShipmentStatus { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        // Currency Information
        public string CurrencySymbol { get; set; } = "$";
        public string CurrencyName { get; set; } = "USD";

        // UI Helpers
        public string StatusClass { get; set; } = "pending";
    }
}
