using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.CfDefPlaceOfLoading;

namespace GCTL.UI.Core.ViewModels.CfDefPlaceOfLoading
{
    public class CfDefPlaceOfLoadingPageViewModel : BaseViewModel
    {
        public CfDefPlaceOfLoadingSetupViewModel Setup { get; set; }
            = new CfDefPlaceOfLoadingSetupViewModel();

        public List<CfDefPlaceOfLoadingSetupViewModel> PlaceOfLoadingList { get; set; }
            = new List<CfDefPlaceOfLoadingSetupViewModel>();
    }
}