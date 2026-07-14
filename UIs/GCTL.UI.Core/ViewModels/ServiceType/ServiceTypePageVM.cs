using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ServiceType;

namespace GCTL_App.ViewModels.MasterSetup.ServiceType
{
    public class ServicePageVM : BaseViewModel
    {
        public ServiceVM Setup { get; set; } = new ServiceVM();
        public List<ServiceVM> List { get; set; } = new List<ServiceVM>();
    }
}