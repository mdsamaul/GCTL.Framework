using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Finance.IncomeStatement
{
    public class IncomeStatementReportVM
    {
        public string CompanyName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromDateFormatted => FromDate.ToString("dd/MM/yyyy");
        public string ToDateFormatted => ToDate.ToString("dd/MM/yyyy");

        // KPI Summary Cards
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetSurplus { get; set; }
        public bool IsSurplus => NetSurplus >= 0;
        public string NetLabel => IsSurplus ? "Net surplus" : "Net deficit";
        public string SurplusMargin { get; set; } = "0.0%";

        // Formatted KPI Values
        public string TotalIncomeFormatted => "৳ " + TotalIncome.ToString("N2");
        public string TotalExpensesFormatted => "৳ " + TotalExpenses.ToString("N2");
        public string NetSurplusFormatted => "৳ " + Math.Abs(NetSurplus).ToString("N2");

        // Statement Rows
        public List<IncomeStatementItemVM> Items { get; set; } = new List<IncomeStatementItemVM>();
        public List<IncomeStatementSpResultDto> RawItems { get; set; } = new List<IncomeStatementSpResultDto>();
    }
}
