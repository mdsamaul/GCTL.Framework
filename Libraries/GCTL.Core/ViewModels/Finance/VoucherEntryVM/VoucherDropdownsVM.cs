using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Finance.VoucherEntryVM
{
    public class VoucherDropdownsVM
    {
        public List<VoucherDropdownItemVM> Companies { get; set; } = new List<VoucherDropdownItemVM>();
        public List<VoucherDropdownItemVM> Branches { get; set; } = new List<VoucherDropdownItemVM>();
        public List<VoucherDropdownStringItemVM> FinancialYears { get; set; } = new List<VoucherDropdownStringItemVM>();
        public List<VoucherTypeDropdownVM> VoucherTypes { get; set; } = new List<VoucherTypeDropdownVM>();
        public List<VoucherAccountHeadDropdownVM> AccountHeads { get; set; } = new List<VoucherAccountHeadDropdownVM>();
    }

    public class VoucherDropdownItemVM
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class VoucherDropdownStringItemVM
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class VoucherTypeDropdownVM
    {
        public string Code { get; set; } = string.Empty; // "1", "2", "3", "4", "5", "6" (VoucherType_Code)
        public string TypeName { get; set; } = string.Empty; // "BRV", "BPV", "CRV", "CPV", "JV", "CV"
        public string Name { get; set; } = string.Empty; // "Bank Receipt Voucher"
        public string Prefix { get; set; } = string.Empty; // "BRV", "BPV", etc.
        public string DisplayText => !string.IsNullOrWhiteSpace(TypeName) ? $"{TypeName} – {Name}" : $"{Code} – {Name}";
    }

    public class VoucherAccountHeadDropdownVM
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string HierarchyPath { get; set; } = string.Empty;
        public string DefaultTrType { get; set; } = "Dr";
        public bool IsCash { get; set; }
        public bool IsBank { get; set; }
    }
}
