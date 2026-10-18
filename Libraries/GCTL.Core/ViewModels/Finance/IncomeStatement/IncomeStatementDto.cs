using System;

namespace GCTL.Core.ViewModels.Finance.IncomeStatement
{
    /// <summary>
    /// Stored Procedure [Rpt_proc_IncomeStatementReportF2] result mapping DTO
    /// </summary>
    public class IncomeStatementSpResultDto
    {
        public int Level { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ParticularName { get; set; } = string.Empty;
        public string Particulars { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public string Section { get; set; } = string.Empty; // "Income", "Expense", "Net"
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Amount { get; set; }
        public bool IsTotal { get; set; }
        public string? BranchCode { get; set; }
    }
}
