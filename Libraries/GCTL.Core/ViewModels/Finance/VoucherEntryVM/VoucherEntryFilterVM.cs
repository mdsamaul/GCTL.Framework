using System;

namespace GCTL.Core.ViewModels.Finance.VoucherEntryVM
{
    public class VoucherEntryFilterVM
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? VoucherType { get; set; }
        public string? Status { get; set; }
        public string? BranchCode { get; set; }
        public string? CompanyCode { get; set; }
        public string? SearchTerm { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } = "VoucherDate";
        public string? SortOrder { get; set; } = "desc";
    }
}
