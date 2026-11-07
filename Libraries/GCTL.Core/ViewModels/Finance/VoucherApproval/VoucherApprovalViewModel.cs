using GCTL.Core.ViewModels;
using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Finance.VoucherApproval
{
    public class VoucherApprovalFilterVM
    {
        public string? CustomerCode { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? VoucherStatus { get; set; } = "Not Approved"; // Not Approved, Approved, Disapproved, All
        
        // DataTables Server-side properties
        public int Draw { get; set; }
        public int Start { get; set; } = 0;
        public int Length { get; set; } = 10;
        public string? SearchValue { get; set; }
        public string? SortColumn { get; set; } = "VoucherDate";
        public string? SortDirection { get; set; } = "DESC";
    }

    public class VoucherApprovalItemVM
    {
        public decimal AutoId { get; set; }
        public string? VoucherEntryCodeNo { get; set; }
        public string? VoucherNo { get; set; }
        public string? VoucherDate { get; set; }
        public string? Narration { get; set; }
        public string? Branch { get; set; }
        public decimal Amount { get; set; }
        public string? JobNo { get; set; }
        public string? HAWB { get; set; }
        public string? BENo { get; set; }
        public string? IsApproved { get; set; }
        public string? ApprovedBy { get; set; }
        public string? ApprovedDate { get; set; }
    }

    public class VoucherApprovalSummaryVM
    {
        public int TotalVouchers { get; set; }
        public decimal TotalAmount { get; set; }
        public int PendingApproval { get; set; }
        public int ApprovedVouchers { get; set; }
    }

    public class VoucherApprovalActionRequestVM
    {
        public List<string> VoucherNos { get; set; } = new List<string>();
        public string Status { get; set; } = "Approved"; // "Approved" or "Disapproved"
    }

    public class VoucherApprovalPageVM
    {
        public VoucherApprovalFilterVM Filter { get; set; } = new VoucherApprovalFilterVM();
        public List<CommonSelectVM> Customers { get; set; } = new List<CommonSelectVM>();
        public VoucherApprovalSummaryVM Summary { get; set; } = new VoucherApprovalSummaryVM();
        public List<CommonSelectVM> VoucherStatuses { get; set; } = new List<CommonSelectVM>
        {
            new CommonSelectVM { Code = "Not Approved", Name = "Not Approved" },
            new CommonSelectVM { Code = "Approved", Name = "Approved" },
            new CommonSelectVM { Code = "Disapproved", Name = "Disapproved" },
            new CommonSelectVM { Code = "All", Name = "All" }
        };
    }
}
