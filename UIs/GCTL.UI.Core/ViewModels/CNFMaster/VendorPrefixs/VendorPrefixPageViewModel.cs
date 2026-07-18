using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.CNFMaster.VendorPrefixs;

namespace GCTL.UI.Core.ViewModels.CNFMaster.VendorPrefixs
{
    public class VendorPrefixPageViewModel : BaseViewModel
    {
        public VendorPrefixSetupVM Setup { get; set; } = new VendorPrefixSetupVM();
    }
}
