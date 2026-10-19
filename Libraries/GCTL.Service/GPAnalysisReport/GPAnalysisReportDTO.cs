using System;
using System.Text.Json.Serialization;

namespace GCTL.Service.GPAnalysisReport
{
    public class GPAnalysisReportDTO
    {
        [JsonPropertyName("slNo")]
        public int SlNo { get; set; }

        [JsonPropertyName("expenseType")]
        public string? ExpenseType { get; set; }

        [JsonPropertyName("expenseTypeID")]
        public string? ExpenseTypeID { get; set; }

        [JsonIgnore]
        public string? ExpenseTypeId { get => ExpenseTypeID; set => ExpenseTypeID = value; }

        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("customerID")]
        public string? CustomerID { get; set; }

        [JsonIgnore]
        public string? CustomerId { get => CustomerID; set => CustomerID = value; }

        [JsonPropertyName("jobNo")]
        public string? JobNo { get; set; }

        [JsonPropertyName("billNo")]
        public string? BillNo { get; set; }

        [JsonPropertyName("billDate")]
        public string? BillDate { get; set; }

        // Billing
        [JsonPropertyName("totalBill")]
        public decimal TotalBill { get; set; }  // Total Bill (BDT)

        [JsonPropertyName("billingCommission")]
        public decimal BillingCommission { get; set; }  // Comm.

        [JsonPropertyName("othersBill")]
        public decimal OthersBill { get; set; }  // Others

        [JsonPropertyName("totalCNFBilling")]
        public decimal TotalCNFBilling { get; set; }  // TOT (C & F)

        [JsonPropertyName("billingTransportBill")]
        public decimal BillingTransportBill { get; set; }  // Transport

        [JsonPropertyName("totalBilling")]
        public decimal TotalBilling { get; set; }  // TOT Bill

        // Costing - C & F Cost
        [JsonPropertyName("costingReceptableAmount")]
        public decimal CostingReceptableAmount { get; set; }  // Rece

        [JsonPropertyName("costingNonReceptableAmount")]
        public decimal CostingNonReceptableAmount { get; set; }  // NON Rece

        [JsonPropertyName("cnfCosting")]
        public decimal CNFCosting { get; set; }  // TOT Cost (C & F Cost)

        // Costing - Transport Cost
        [JsonPropertyName("costingSwiftTransportAmount")]
        public decimal CostingSwiftTransportAmount { get; set; } // Swift

        [JsonPropertyName("costingHairTransportAmount")]
        public decimal CostingHairTransportAmount { get; set; }  // Hair

        [JsonPropertyName("costingTransport")]
        public decimal CostingTransport { get; set; }  // TOT (Transport)

        [JsonPropertyName("totalCosting")]
        public decimal TotalCosting { get; set; }  // TOT Cost

        // PNL - GP
        [JsonPropertyName("cnfGP")]
        public decimal CNFGP { get; set; }   // C & F

        [JsonPropertyName("transportGP")]
        public decimal TransportGP { get; set; }  // TRANS

        [JsonPropertyName("totalGP")]
        public decimal TotalGP { get; set; }      // TOT

        // PNL - GP%
        [JsonPropertyName("cnfGPPercent")]
        public string? CNFGPPercent { get; set; }  // C & F

        [JsonPropertyName("transportGPPercent")]
        public string? TransportGPPercent { get; set; } // TRANS

        [JsonPropertyName("totalGPPercent")]
        public string? TotalGPPercent { get; set; }   // G. GP

        // Totals from Stored Procedure
        public decimal CustomerBillingTransportBill { get; set; }
        public decimal GrandTotalBillingTransportBill { get; set; }
        public decimal CustomerBillingCommission { get; set; }
        public decimal GrandTotalBillingCommission { get; set; }
        public decimal CustomerOthersBill { get; set; }
        public decimal GrandTotalOthersBill { get; set; }
        public decimal CustomerTotalCNFBilling { get; set; }
        public decimal GrandTotalTotalCNFBilling { get; set; }
        public decimal CustomerCostingReceptableAmount { get; set; }
        public decimal GrandTotalCostingReceptableAmount { get; set; }
        public decimal CustomerCostingNonReceptableAmount { get; set; }
        public decimal GrandTotalCostingNonReceptableAmount { get; set; }
        public decimal CustomerCostingHairTransportAmount { get; set; }
        public decimal GrandTotalCostingHairTransportAmount { get; set; }
        public decimal CustomerCostingSwiftTransportAmount { get; set; }
        public decimal GrandTotalCostingSwiftTransportAmount { get; set; }
        public decimal CustomerCNFCosting { get; set; }
        public decimal GrandTotalCNFCosting { get; set; }
        public decimal CustomerCostingTransport { get; set; }
        public decimal GrandTotalCostingTransport { get; set; }
        public decimal CustomerTotalBilling { get; set; }
        public decimal GrandTotalTotalBilling { get; set; }
        public decimal CustomerTotalCosting { get; set; }
        public decimal GrandTotalTotalCosting { get; set; }
        public decimal CustomerTotalGP { get; set; }
        public decimal GrandTotalTotalGP { get; set; }
        public decimal CustomerCNFGP { get; set; }
        public decimal GrandTotalCNFGP { get; set; }
        public decimal CustomerTransportGP { get; set; }
        public decimal GrandTotalTransportGP { get; set; }
        public string? CustomerTotalGPPercent { get; set; }
        public string? GrandTotalGPPercent { get; set; }
        public string? CustomerTransportGPPercent { get; set; }
        public string? GrandTotalTransportGPPercent { get; set; }
        public string? CustomerCNFGPPercent { get; set; }
        public string? GrandTotalCNFGPPercent { get; set; }
        public int CountCustomerBill { get; set; }
        public int CountTotalBill { get; set; }
    }

    public class GPAnalysisReportFilter
    {
        public string? ShipmentMode { get; set; }
        public string? CustomerId { get; set; }
        public string? DateUse { get; set; } = "Y";
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }

        // DataTables server-side parameters
        public int? Draw { get; set; }
        public int? Start { get; set; }
        public int? Length { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortColumn { get; set; }
        public string? SortOrder { get; set; }
    }

    public class GPAnalysisReportFilterOptionsDTO
    {
        public List<GCTL.Core.ViewModels.Common.CommonSelectModel> ShipmentModes { get; set; } = new();
        public List<GCTL.Core.ViewModels.Common.CommonSelectModel> Customers { get; set; } = new();
    }

    public class GPAnalysisReportSummaryDTO
    {
        public int TotalBills { get; set; }
        public decimal NetBillValue { get; set; }
        public decimal TotalCosting { get; set; }
        public decimal Profit { get; set; }
        public string GPPercent { get; set; } = "0.00%";
    }
}


