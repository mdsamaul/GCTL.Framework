using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Departments;

namespace GCTL.UI.Core.ViewModels.Departments
{
    public class DepartmentPageViewModel : BaseViewModel
    {
        public DepartmentSetupViewModel Setup { get; set; } = new DepartmentSetupViewModel();
        public List<DepartmentSetupViewModel> DepartmentList { get; set; } = new List<DepartmentSetupViewModel>();
        public DynamicGridModel? DynamicGrid { get; set; }
    }
}
