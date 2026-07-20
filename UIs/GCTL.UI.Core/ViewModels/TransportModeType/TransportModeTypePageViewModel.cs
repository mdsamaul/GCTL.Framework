using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.TransportModeType;
using GCTL.Core.ViewModels.TransportType;

namespace GCTL.UI.Core.ViewModels.TransportModeType
{
    public class TransportModeTypePageViewModel : BaseViewModel
    {
        public TransportModeTypeSetupViewModel Setup { get; set; } = new TransportModeTypeSetupViewModel();
        public List<TransportModeTypeSetupViewModel> TransportModeTypeList { get; set; } = new List<TransportModeTypeSetupViewModel>();
    }
}
