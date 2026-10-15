using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Finance.OpeningBalancesVM
{
    public class OpeningBalanceListResultVM
    {
        public List<OpeningBalanceVM> Items { get; set; } = new List<OpeningBalanceVM>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public OpeningBalanceSummaryVM Summary { get; set; } = new OpeningBalanceSummaryVM();
    }
}
