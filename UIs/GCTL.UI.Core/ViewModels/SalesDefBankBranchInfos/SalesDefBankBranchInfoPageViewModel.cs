using System.Collections.Generic;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.SalesDefBankBranchInfos;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.ViewModels.SalesDefBankBranchInfos
{
    public class SalesDefBankBranchInfoPageViewModel : BaseViewModel
    {
        public SalesDefBankBranchInfoSetupViewModel Setup { get; set; } = new SalesDefBankBranchInfoSetupViewModel();
        public List<SalesDefBankBranchInfoSetupViewModel> TableList { get; set; } = new List<SalesDefBankBranchInfoSetupViewModel>();
        public DynamicGridModel? DynamicGrid { get; set; }
        public List<SelectListItem> BankDD { get; set; } = new List<SelectListItem>();
    }
}
