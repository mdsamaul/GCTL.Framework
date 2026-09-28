using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.ClearAndF.BillEntry
{
    public class JobBillEntryReportFilterVM
    {
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? ShipmentModes { get; set; }
        public string? CustomerIds { get; set; }
        public string? BillNos { get; set; }
        public string? SelectedBillNos { get; set; }

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

    public class JobBillEntryReportItemDTO
    {
        public int Sl { get; set; }
        public string BillNo { get; set; } = string.Empty;
        public string BillDate { get; set; } = string.Empty;
        public string JobNo { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public decimal ReceiptableAmount { get; set; }
        public decimal NonReceiptableAmount { get; set; }
        public decimal TransportAmount { get; set; }
        public decimal AgencyCommission { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Deduction { get; set; }
        public decimal NetAmount { get; set; }
    }

    public class JobBillEntryReportSummaryDTO
    {
        public int TotalBillCount { get; set; }
        public decimal NetServiceValue { get; set; }
        public decimal TotalDueOutstanding { get; set; }
        public decimal LastServiceValue { get; set; }
        public string? LastServiceDate { get; set; }
    }

    public class JobBillEntryReportFilterOptionsDTO
    {
        public List<GCTL.Core.ViewModels.Common.CommonSelectModel> ShipmentModes { get; set; } = new();
        public List<GCTL.Core.ViewModels.Common.CommonSelectModel> Customers { get; set; } = new();
        public List<string> BillNos { get; set; } = new();
    }
}

