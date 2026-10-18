using System;
using System.Collections.Generic;
using GCTL.Core.ViewModels;

namespace GCTL.Core.ViewModels.Finance.CashFlow
{
    public class CashFlowFilterVM
    {
        public string? Main_CompanyCode { get; set; } = string.Empty;
        public string? BranchCode { get; set; } = "All";
        public DateTime? FromDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime? ToDate { get; set; } = DateTime.Now;
    }

    public class CashFlowReportVM
    {
        public CashFlowFilterVM Filter { get; set; } = new CashFlowFilterVM();
        public CashFlowSummaryVM Summary { get; set; } = new CashFlowSummaryVM();
        public List<CashFlowActivityGroupDto> Activities { get; set; } = new List<CashFlowActivityGroupDto>();
        public List<CommonSelectVM> Companies { get; set; } = new List<CommonSelectVM>();
        public List<CommonSelectVM> Branches { get; set; } = new List<CommonSelectVM>();
        public string CompanyName { get; set; } = "Greenfield International School";
    }
}
