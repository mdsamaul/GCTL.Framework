using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.PortType;
using GCTL.Core.ViewModels.TransportType;

namespace GCTL.UI.Core.ViewModels.TransportType
{
    public class TransportTypePageViewModel : BaseViewModel
    {
        public TransportTypeSetupViewModel Setup { get; set; } = new TransportTypeSetupViewModel();
        public List<TransportTypeSetupViewModel> TransportTypeList { get; set; } = new List<TransportTypeSetupViewModel>();
    }
}
