using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.CustomerType;

namespace GCTL.UI.Core.ViewModels.CustomerType
{
    public class CustomerTypePageViewModel : BaseViewModel
    {
        public CustomerTypeSetupViewModel Setup { get; set; } = new CustomerTypeSetupViewModel();
        public List<CustomerTypeSetupViewModel> CustomerTypeList { get; set; } = new List<CustomerTypeSetupViewModel>();
        public DynamicGridModel? DynamicGrid { get; set; }
    }
}
