using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ImporterInfo;

namespace GCTL.UI.Core.ViewModels.ImporterInfo
{
    public class ImporterInfoPageViewModel : BaseViewModel
    {
        public ImporterInfoSetupViewModel Setup { get; set; } = new ImporterInfoSetupViewModel();
        public List<ImporterInfoSetupViewModel> ImporterInfoList { get; set; } = new List<ImporterInfoSetupViewModel>();
    }
}
