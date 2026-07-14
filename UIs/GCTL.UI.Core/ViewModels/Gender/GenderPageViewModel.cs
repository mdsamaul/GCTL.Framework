using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Gender;

namespace GCTL.UI.Core.ViewModels.Gender
{
    public class GenderPageViewModel:BaseViewModel
    {
        public GenderSetupViewModel Setup { get; set; } = new GenderSetupViewModel();
        public List<GenderSetupViewModel> GenderList { get; set; } = new List<GenderSetupViewModel>();
    }
}
