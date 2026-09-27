using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ManageNoticeEntries;

namespace GCTL.UI.Core.ViewModels.ManageNotices
{
    public class ManageNoticePageViewModel : BaseViewModel
    {
        public ManageNoticeSetupViewModel Setup { get; set; } = new ManageNoticeSetupViewModel();
    }
}
