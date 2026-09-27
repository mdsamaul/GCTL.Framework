using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.ClearAndF.BillEntry
{
    public class JobBillPrintVM
    {
        // Header
        public string CompanyName { get; set; } = "Formosa Logistics Limited";
        public string CompanyAddress { get; set; } = "21, Hazaribagh, Dhaka-1209";

        // Bill Meta
        public string BillNo { get; set; } = string.Empty;
        public string BillDate { get; set; } = string.Empty;
        public string JobNo { get; set; } = string.Empty;

        // Customer
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;

        // Material
        public string MaterialDescription { get; set; } = string.Empty;

        // Column 1
        public string Quantity { get; set; } = string.Empty;
        public string PackingQuantity { get; set; } = string.Empty;
        public string BlNo { get; set; } = string.Empty;
        public string BlDate { get; set; } = string.Empty;
        public string BeNo { get; set; } = string.Empty;
        public string BeDate { get; set; } = string.Empty;

        // Column 2
        public string Weight { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoiceDate { get; set; } = string.Empty;
        public string ExpNo { get; set; } = string.Empty;
        public string ExpDate { get; set; } = string.Empty;
        public string HawbNo { get; set; } = string.Empty;

        // Column 3
        public string Destination { get; set; } = string.Empty;
        public string DocRecvDate { get; set; } = string.Empty;
        public string ArrivalDate { get; set; } = string.Empty;
        public string DeliveryDate { get; set; } = string.Empty;
        public string LcNo { get; set; } = string.Empty;
        public string LcDate { get; set; } = string.Empty;

        // Values Bar
        public string Currency { get; set; } = "USD";
        public decimal? InvoiceValue { get; set; }
        public decimal? AssessableValue { get; set; }

        // Categories of Charges
        public List<JobBillPrintChargeItem> ReceiptableItems { get; set; } = new();
        public decimal ReceiptableSubtotal { get; set; }

        public List<JobBillPrintChargeItem> NonReceiptableItems { get; set; } = new();
        public decimal NonReceiptableSubtotal { get; set; }

        public List<JobBillPrintChargeItem> TransportItems { get; set; } = new();
        public decimal TransportSubtotal { get; set; }

        public List<JobBillPrintChargeItem> AgencyCommissionItems { get; set; } = new();
        public decimal AgencyCommissionSubtotal { get; set; }

        // Summary
        public decimal TotalAmount { get; set; }
        public decimal? Deductions { get; set; }
        public decimal NetDue { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string InWords { get; set; } = string.Empty;
    }

    public class JobBillPrintChargeItem
    {
        public string Particulars { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
