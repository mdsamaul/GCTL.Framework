using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.VendorPrefix;

namespace GCTL.UI.Core.ViewModels.VendorPrefix
{
    public class VendorPrefixPageViewModel : BaseViewModel
    {
        public VendorPrefixSetupViewModel Setup { get; set; } = new VendorPrefixSetupViewModel();
        public List<VendorPrefixSetupViewModel> VendorPrefixList { get; set; } = new List<VendorPrefixSetupViewModel>();
    }
}
