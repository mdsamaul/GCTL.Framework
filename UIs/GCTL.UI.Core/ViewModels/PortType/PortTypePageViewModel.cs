using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.PortType;
using GCTL.Core.ViewModels.SupplierType;

namespace GCTL.UI.Core.ViewModels.PortType
{
    public class PortTypePageViewModel : BaseViewModel
    {
        public PortTypeSetupViewModel Setup { get; set; } = new PortTypeSetupViewModel();
        public List<PortTypeSetupViewModel> PortTypeList { get; set; } = new List<PortTypeSetupViewModel>();
    }
}
