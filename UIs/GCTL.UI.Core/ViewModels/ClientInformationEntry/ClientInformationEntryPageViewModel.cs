using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ClientInformationEntry;

namespace GCTL.UI.Core.ViewModels.ClientInformationEntry
{
    public class ClientInformationEntryPageViewModel : BaseViewModel
    {
        public ClientInformationEntrySetupViewModel Setup { get; set; } = new ClientInformationEntrySetupViewModel();
        public List<ClientInformationEntrySetupViewModel> ClientInformationEntryList { get; set; } = new List<ClientInformationEntrySetupViewModel>();
    }
}
