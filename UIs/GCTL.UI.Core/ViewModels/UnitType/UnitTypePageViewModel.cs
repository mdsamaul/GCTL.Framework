using System.Collections.Generic;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.UnitType;

namespace GCTL.UI.Core.ViewModels.UnitType
{
    public class UnitTypePageViewModel : BaseViewModel
    {
        public UnitTypeSetupViewModel Setup { get; set; } = new UnitTypeSetupViewModel();
        public List<UnitTypeSetupViewModel> TableList { get; set; } = new List<UnitTypeSetupViewModel>();
        public DynamicGridModel? DynamicGrid { get; set; }
    }
}
