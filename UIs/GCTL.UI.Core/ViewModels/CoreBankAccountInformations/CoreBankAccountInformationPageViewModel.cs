using System.Collections.Generic;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.CoreBankAccountInformations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.ViewModels.CoreBankAccountInformations
{
    public class CoreBankAccountInformationPageViewModel : BaseViewModel
    {
        public CoreBankAccountInformationSetupViewModel Setup { get; set; } = new CoreBankAccountInformationSetupViewModel();
        public List<CoreBankAccountInformationSetupViewModel> TableList { get; set; } = new List<CoreBankAccountInformationSetupViewModel>();
        public DynamicGridModel? DynamicGrid { get; set; }
        public List<SelectListItem> BankDD { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> BranchDD { get; set; } = new List<SelectListItem>();
    }
}
