using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Finance.VoucherEntryVM
{
    public class VoucherEntryListResultVM
    {
        public List<VoucherEntryVM> Items { get; set; } = new List<VoucherEntryVM>();
        public VoucherEntrySummaryVM Summary { get; set; } = new VoucherEntrySummaryVM();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }
}
