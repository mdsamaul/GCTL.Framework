using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ShipmentModeLedgerSetup;

namespace GCTL.UI.Core.ViewModels.ShipmentModeLedgerSetup
{
    public class ShipmentModeLedgerSetupPageViewModel : BaseViewModel
    {
        public ShipmentModeLedgerSetupSetupViewModel Setup { get; set; } = new ShipmentModeLedgerSetupSetupViewModel();
        public List<ShipmentModeLedgerSetupSetupViewModel> ShipmentModeLedgerSetupList { get; set; } = new List<ShipmentModeLedgerSetupSetupViewModel>();
    }
}
