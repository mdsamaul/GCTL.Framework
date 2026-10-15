using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Finance.OpeningBalancesVM
{
    public class OpeningBalanceDropdownsVM
    {
        public List<DropdownItemVM> Companies { get; set; } = new List<DropdownItemVM>();
        public List<DropdownItemVM> Branches { get; set; } = new List<DropdownItemVM>();
        public List<DropdownStringItemVM> FinancialYears { get; set; } = new List<DropdownStringItemVM>();
        public List<AccountHeadDropdownVM> AccountHeads { get; set; } = new List<AccountHeadDropdownVM>();
    }

    public class DropdownItemVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class DropdownStringItemVM
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class AccountHeadDropdownVM
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string DefaultTrType { get; set; } = "Dr";
    }
}
