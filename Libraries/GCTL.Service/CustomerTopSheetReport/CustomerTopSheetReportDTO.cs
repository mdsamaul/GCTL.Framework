using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using GCTL.Core.ViewModels.Common;

namespace GCTL.Service.CustomerTopSheetReport
{
    public class CustomerTopSheetFilter
    {
        [JsonPropertyName("companyCode")]
        public string? CompanyCode { get; set; } = string.Empty;

        [JsonPropertyName("branchCode")]
        public string? BranchCode { get; set; } = string.Empty;

        [JsonPropertyName("fromDate")]
        public string? FromDate { get; set; }

        [JsonPropertyName("toDate")]
        public string? ToDate { get; set; }

        [JsonPropertyName("customerId")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("shipmentModeId")]
        public string? ShipmentModeId { get; set; }

        [JsonPropertyName("jobNo")]
        public string? JobNo { get; set; }

        [JsonPropertyName("billNo")]
        public string? BillNo { get; set; }

        [JsonPropertyName("exportFormat")]
        public string? ExportFormat { get; set; } = "Preview";

        // DataTables pagination parameters
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

    public class CustomerTopSheetFilterOptionsDTO
    {
        public List<CommonSelectModel> Companies { get; set; } = new();
        public List<CommonSelectModel> Branches { get; set; } = new();
        public List<CommonSelectModel> ShipmentModes { get; set; } = new();
        public List<CommonSelectModel> Customers { get; set; } = new();
        public List<string> BillNos { get; set; } = new();
        public List<string> JobNos { get; set; } = new();
    }

    /// <summary>
    /// Dashboard Top 5 KPI Cards (Total Customers, Total Bills, Total Bill Amount, Transport & Labour, Commission)
    /// </summary>
    public class CustomerTopSheetSummaryDTO
    {
        [JsonPropertyName("totalCustomers")]
        public int TotalCustomers { get; set; }

        [JsonPropertyName("totalBills")]
        public int TotalBills { get; set; }

        [JsonPropertyName("totalBillAmount")]
        public decimal TotalBillAmount { get; set; }

        [JsonPropertyName("totalTransportLabour")]
        public decimal TotalTransportLabour { get; set; }

        [JsonPropertyName("totalCommission")]
        public decimal TotalCommission { get; set; }
    }

    public class CustomerTopSheetCustomerDTO
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CompanyName { get; set; } = "Formosa Logistics Limited.";
        public string HeadOfficeAddress { get; set; } = "Safura tower (6th floor), 20, Kemal Ataturk Avenue, Banani C/A, Dhaka 1213, Bangladesh.";
        public string ContactPhoneFax { get; set; } = "Tel: 880-2-9881830, 9885748, Fax: 880-2-8822843";
        public string Attn { get; set; } = "ACCOUNTS DEPARTMENT";
        public string Ref { get; set; } = "SUBMISSION OF C&F BILLS.";
        public string ReportDate { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");
        public int TotalBills { get; set; }
        public decimal TotalDti { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalCustomsMisc { get; set; }
        public decimal TotalWarehouseRent { get; set; }
        public decimal TotalFfNoc { get; set; }
        public decimal TotalLabour { get; set; }
        public decimal TotalOthersMisc { get; set; }
        public decimal TotalAirFreight { get; set; }
        public decimal TotalTransport { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal GrandTotalAmount { get; set; }
        public string AmountInWords { get; set; } = string.Empty;
        public List<CustomerTopSheetBillItemDTO> Bills { get; set; } = new();
    }

    public class CustomerTopSheetBillItemDTO
    {
        [JsonPropertyName("slNo")]
        public long SlNo { get; set; }

        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("billNo")]
        public string BillNo { get; set; } = string.Empty;

        [JsonPropertyName("jobNo")]
        public string JobNo { get; set; } = string.Empty;

        [JsonPropertyName("lcLcaNo")]
        public string LcLcaNo { get; set; } = string.Empty;

        [JsonPropertyName("invoiceNo")]
        public string InvoiceNo { get; set; } = string.Empty;

        [JsonPropertyName("invoiceDate")]
        public string InvoiceDate { get; set; } = string.Empty;

        [JsonPropertyName("beRegNo")]
        public string BeRegNo { get; set; } = string.Empty;

        [JsonPropertyName("beRegDate")]
        public string BeRegDate { get; set; } = string.Empty;

        [JsonPropertyName("dtiAssociationServiceCharge")]
        public decimal DtiAssociationServiceCharge { get; set; }

        [JsonPropertyName("vatGlobalTax")]
        public decimal VatGlobalTax { get; set; }

        [JsonPropertyName("customsMisc")]
        public decimal CustomsMisc { get; set; }

        [JsonPropertyName("warehouseRent")]
        public decimal WarehouseRent { get; set; }

        [JsonPropertyName("freightForwarderNOC")]
        public decimal FreightForwarderNOC { get; set; }

        [JsonPropertyName("labourCharge")]
        public decimal LabourCharge { get; set; }

        [JsonPropertyName("othersMisc")]
        public decimal OthersMisc { get; set; }

        [JsonPropertyName("airFreightCharge")]
        public decimal AirFreightCharge { get; set; }

        [JsonPropertyName("transportBill")]
        public decimal TransportBill { get; set; }

        [JsonPropertyName("commission")]
        public decimal Commission { get; set; }

        [JsonPropertyName("totalBillTk")]
        public decimal TotalBillTk { get; set; }
    }

    public class CustomerTopSheetGridResult
    {
        public int Draw { get; set; }
        public int TotalCount { get; set; }
        public int FilteredCount { get; set; }
        public List<CustomerTopSheetBillItemDTO> Data { get; set; } = new();
        public CustomerTopSheetSummaryDTO Summary { get; set; } = new();
    }
}
