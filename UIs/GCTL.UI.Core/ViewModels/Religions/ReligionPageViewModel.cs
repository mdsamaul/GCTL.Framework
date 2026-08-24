using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Religions;

namespace GCTL.UI.Core.ViewModels.Religions
{
    public class ReligionPageViewModel : BaseViewModel
    {
        public ReligionSetupViewModel Setup { get; set; } = new ReligionSetupViewModel();
        public List<ReligionSetupViewModel> ReligionList { get; set; } = new List<ReligionSetupViewModel>();
        public DynamicGridModel? DynamicGrid { get; set; }
    }
}
