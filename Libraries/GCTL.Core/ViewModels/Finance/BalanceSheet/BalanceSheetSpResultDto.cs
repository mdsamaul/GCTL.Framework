using System;

namespace GCTL.Core.ViewModels.Finance.BalanceSheet
{
    /// <summary>
    /// Stored Procedure [Rpt_proc_BalanceSheetReportF2] result mapping DTO
    /// </summary>
    public class BalanceSheetSpResultDto
    {
        public int Level { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ParticularName { get; set; } = string.Empty;
        public string Particulars { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public string Section { get; set; } = string.Empty; // "Assets", "Equities & Liabilities", "Surplus"
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Amount { get; set; }
        public bool IsTotal { get; set; }
        public bool IsHighlighted { get; set; }
        public string? BranchCode { get; set; }
    }
}
