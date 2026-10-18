using System;

namespace GCTL.Core.ViewModels.Finance.TrialBalance
{
    /// <summary>
    /// Stored Procedure [Rpt_proc_TrialBalanceReportF2] result mapping DTO
    /// </summary>
    public class TrialBalanceSpResultDto
    {
        public int Level { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ParticularName { get; set; } = string.Empty;
        public string Particulars { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public decimal OpeningDebit { get; set; }
        public decimal OpeningCredit { get; set; }
        public decimal PeriodDebit { get; set; }
        public decimal PeriodCredit { get; set; }
        public decimal ClosingDebit { get; set; }
        public decimal ClosingCredit { get; set; }
        public string? BranchCode { get; set; }

        // Backwards compatibility properties
        public string GeneralLedgerName => ParticularName;
        public decimal OpeningBalance => OpeningDebit - OpeningCredit;
        public decimal Debit => ClosingDebit;
        public decimal Credit => ClosingCredit;
        public decimal DebitBalance => ClosingDebit;
        public decimal CreditBalance => ClosingCredit;
    }

    /// <summary>
    /// 5-Level Chart of Accounts hierarchical item for rollup display
    /// </summary>
    public class TrialBalanceHierarchyItemDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; } // 1: Group, 2: Control, 3: SubControl, 4: SubSubsidiary, 5: GeneralLedger
        public string? ParentCode { get; set; }
        public decimal OpeningDebit { get; set; }
        public decimal OpeningCredit { get; set; }
        public decimal PeriodDebit { get; set; }
        public decimal PeriodCredit { get; set; }
        public decimal ClosingDebit { get; set; }
        public decimal ClosingCredit { get; set; }
    }
}
