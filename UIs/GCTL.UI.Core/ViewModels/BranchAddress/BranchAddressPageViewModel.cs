using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.BranchAddress;
using GCTL.Core.ViewModels.VendorInfo;

namespace GCTL.UI.Core.ViewModels.BranchAddress
{
    public class BranchAddressPageViewModel : BaseViewModel
    {
        public BranchAddressSetupViewModel Setup { get; set; } = new BranchAddressSetupViewModel();
        public List<BranchAddressSetupViewModel> BranchAddressList { get; set; } = new List<BranchAddressSetupViewModel>();
    }
}
