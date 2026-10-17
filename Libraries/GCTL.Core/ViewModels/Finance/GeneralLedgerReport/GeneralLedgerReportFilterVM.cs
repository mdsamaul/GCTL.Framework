using System;

namespace GCTL.Core.ViewModels.Finance.GeneralLedgerReport
{
    public class GeneralLedgerReportFilterVM
    {
        public string? CompanyCode { get; set; }
        public string? BranchCode { get; set; }
        public string? GeneralLedgerCodeNo { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
