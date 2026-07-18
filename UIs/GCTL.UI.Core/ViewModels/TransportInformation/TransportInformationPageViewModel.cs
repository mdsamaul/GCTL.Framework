using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.PortName;
using GCTL.Core.ViewModels.TransportInformation;

namespace GCTL.UI.Core.ViewModels.TransportInformation
{
    public class TransportInformationPageViewModel : BaseViewModel
    {
        public TransportInformationSetupViewModel Setup { get; set; } = new TransportInformationSetupViewModel();
        public List<TransportInformationSetupViewModel> TransportList { get; set; } = new List<TransportInformationSetupViewModel>();
    }
}
