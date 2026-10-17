using System;

namespace GCTL.Core.ViewModels.Finance.BalanceSheet
{
    public class BalanceSheetItemVM
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayText => string.IsNullOrWhiteSpace(Code) ? Name : $"{Code} – {Name}";
        public int Level { get; set; } // 1: Level 1 Header, 2: Control, 3: SubControl, 4: SubSubsidiary, 5: Leaf
        public string RowClass => Level switch
        {
            1 => "l1",
            2 => "l2",
            3 => "l3",
            _ => "lf"
        };
        public string Section { get; set; } = string.Empty; // "Assets", "Liabilities", "Equity", "Surplus"
        public decimal Amount { get; set; }
        public string AmountFormatted { get; set; } = string.Empty;
        public bool IsTotal { get; set; }
        public bool IsHighlighted { get; set; }
    }
}
