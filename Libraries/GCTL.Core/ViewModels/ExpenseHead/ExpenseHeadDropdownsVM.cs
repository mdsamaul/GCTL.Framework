using System.Collections.Generic;
using GCTL.Core.ViewModels.Common;

namespace GCTL.Core.ViewModels.ExpenseHead
{
    public class ExpenseHeadDropdownsVM
    {
        public List<CommonSelectModel> ShipmentModes { get; set; } = new();
        public List<CommonSelectModel> ServiceProviders { get; set; } = new();
    }
}

