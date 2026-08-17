using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ExporterInfo;
using GCTL.Core.ViewModels.ImporterInfo;

namespace GCTL.UI.Core.ViewModels.ExporterInfo
{
    public class ExporterInfoPageViewModel : BaseViewModel
    {
        public ExporterInfoSetupViewModel Setup { get; set; } = new ExporterInfoSetupViewModel();
        public List<ExporterInfoSetupViewModel> ExporterInfofoList { get; set; } = new List<ExporterInfoSetupViewModel>();
    }
}
