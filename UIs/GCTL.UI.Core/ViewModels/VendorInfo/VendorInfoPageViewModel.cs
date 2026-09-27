using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.VendorInfo;

namespace GCTL.UI.Core.ViewModels.VendorInfo
{
    public class VendorInfoPageViewModel : BaseViewModel
    {
        public VendorInfoSetupViewModel Setup { get; set; } = new VendorInfoSetupViewModel();
        public List<VendorInfoSetupViewModel> VendorInfoList { get; set; } = new List<VendorInfoSetupViewModel>();
        public TabGroupModel TabModel { get; set; }
    }
}
