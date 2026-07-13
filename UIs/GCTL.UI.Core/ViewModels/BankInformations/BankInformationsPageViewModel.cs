using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.BankInformations;

namespace GCTL.UI.Core.ViewModels.BankInformations
{
    public class BankInformationsPageViewModel:BaseViewModel
    {
        public BankInformationsSetupViewModel Setup { get; set; } = new BankInformationsSetupViewModel();
        public List<BankInformationsSetupViewModel> tableDataList { get; set; } = new List<BankInformationsSetupViewModel>();
    }
}
