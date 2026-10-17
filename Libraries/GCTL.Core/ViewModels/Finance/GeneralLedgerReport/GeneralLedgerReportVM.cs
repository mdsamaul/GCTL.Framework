using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Finance.GeneralLedgerReport
{
    public class GeneralLedgerReportVM
    {
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string HierarchyPath { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromDateFormatted => FromDate.ToString("dd/MM/yyyy");
        public string ToDateFormatted => ToDate.ToString("dd/MM/yyyy");
        public string PeriodFormatted => $"{FromDateFormatted} to {ToDateFormatted}";

        // KPI Summary Cards
        public decimal OpeningBalance { get; set; }
        public string OpeningBalanceFormatted { get; set; } = "0.00";
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
        public string ClosingBalanceFormatted { get; set; } = "0.00";

        // Transaction Rows
        public List<GeneralLedgerItemVM> Items { get; set; } = new List<GeneralLedgerItemVM>();
    }
}
