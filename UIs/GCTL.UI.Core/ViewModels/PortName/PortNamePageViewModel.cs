using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.PortName;

namespace GCTL.UI.Core.ViewModels.PortName
{
    public class PortNamePageViewModel : BaseViewModel
    {
        public PortNameSetupViewModel Setup { get; set; } = new PortNameSetupViewModel();
        public List<PortNameSetupViewModel> PortNameList { get; set; } = new List<PortNameSetupViewModel>();
    }
}
