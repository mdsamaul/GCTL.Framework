using System;

namespace GCTL.Core.ViewModels.Finance.OpeningBalancesVM
{
    public class OpeningBalanceFilterVM
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? FinancialCodeNo { get; set; }
        public string? GeneralLedgerCodeNo { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SortColumn { get; set; } = "ComOpeningBalanceCode";
        public string? SortOrder { get; set; } = "asc";
    }
}
