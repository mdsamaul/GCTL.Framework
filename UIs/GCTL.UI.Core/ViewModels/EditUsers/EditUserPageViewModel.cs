using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.EditUserVM;

namespace GCTL.UI.Core.ViewModels.EditUsers
{
    public class EditUserPageViewModel : BaseViewModel
    {
        public EditUserSetupViewModel Setup { get; set; } = new EditUserSetupViewModel();
    }
}

