using System;

namespace GCTL.Core.ViewModels.Finance.IncomeStatement
{
    public class IncomeStatementFilterVM
    {
        public string? CompanyCode { get; set; }
        public string? BranchCode { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
