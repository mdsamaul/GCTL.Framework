using System.Collections.Generic;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.CurrencyType;

namespace GCTL.UI.Core.ViewModels.CurrencyType
{
    public class CurrencyTypePageViewModel : BaseViewModel
    {
        public CurrencyTypeSetupViewModel Setup { get; set; } = new CurrencyTypeSetupViewModel();
        public List<CurrencyTypeSetupViewModel> tableDataList { get; set; } = new List<CurrencyTypeSetupViewModel>();
        public DynamicGridModel? DynamicGrid { get; set; }
    }
}
