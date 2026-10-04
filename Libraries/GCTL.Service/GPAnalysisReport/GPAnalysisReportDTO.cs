using System;

namespace GCTL.Service.GPAnalysisReport
{
    public class GPAnalysisReportDTO
    {
        public int SlNo { get; set; }
        public string? ExpenseType { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerID { get; set; }
        public string? JobNo { get; set; }
        public string? BillNo { get; set; }
        public string? BillDate { get; set; }

        // Billing
        public decimal TotalBill { get; set; }           // Total Bill (BDT)
        public decimal BillingCommission { get; set; }   // Comm.
        public decimal OthersBill { get; set; }          // Others
        public decimal TotalCNFBilling { get; set; }     // TOT (C & F)
        public decimal BillingTransportBill { get; set; }// Transport
        public decimal TotalBilling { get; set; }        // TOT Bill

        // Costing - C & F Cost
        public decimal CostingReceptableAmount { get; set; }    // Rece
        public decimal CostingNonReceptableAmount { get; set; } // NON Rece
        public decimal CNFCosting { get; set; }                 // TOT Cost (C & F Cost)

        // Costing - Transport Cost
        public decimal CostingSwiftTransportAmount { get; set; } // Swift
        public decimal CostingHairTransportAmount { get; set; }  // Hair
        public decimal CostingTransport { get; set; }            // TOT (Transport)
        public decimal TotalCosting { get; set; }                // TOT Cost

        // PNL - GP
        public decimal CNFGP { get; set; }        // C & F
        public decimal TransportGP { get; set; }  // TRANS
        public decimal TotalGP { get; set; }      // TOT

        // PNL - GP%
        public string? CNFGPPercent { get; set; }       // C & F
        public string? TransportGPPercent { get; set; } // TRANS
        public string? TotalGPPercent { get; set; }     // G. GP

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

    public class GPAnalysisReportSummaryDTO
    {
        public int TotalBills { get; set; }
        public decimal NetBillValue { get; set; }
        public decimal TotalCosting { get; set; }
        public decimal Profit { get; set; }
        public string GPPercent { get; set; } = "0.00%";
    }
}
