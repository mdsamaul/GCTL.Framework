using System;

namespace GCTL.Core.ViewModels.Finance.IncomeStatement
{
    public class IncomeStatementItemVM
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string DisplayText => string.IsNullOrWhiteSpace(Code) ? Name : $"{Code} – {Name}";
        public int Level { get; set; } // 1: Category Header (Income/Expenses), 2: Control, 3: SubControl, 4: SubSubsidiary, 5: Leaf
        public string RowClass
        {
            get
            {
                if (IsTotal) return "tot";
                return Level switch
                {
                    1 => "l1",
                    2 => "l2",
                    3 => "l3",
                    _ => "lf"
                };
            }
        }
        public string Section { get; set; } = string.Empty; // "Income", "Expense", "Net"
        public decimal Amount { get; set; }
        public string AmountFormatted { get; set; } = string.Empty;
        public bool IsTotal { get; set; }
    }
}
