using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Companies;
using GCTL.Data.Models;

namespace GCTL.UI.Core.ViewModels.Companies
{
    public class CompanyPageViewModel : BaseViewModel
    {
        public CompanySetupViewModel Setup { get; set; } = new CompanySetupViewModel();
        public List<CompanySetupViewModel> CompanyList { get; set; } = new List<CompanySetupViewModel>();
        public DynamicGridModel? DynamicGrid { get; set; }
    }
}

