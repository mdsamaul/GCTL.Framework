using System.Collections.Generic;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.PaymentModes;

namespace GCTL.UI.Core.ViewModels.PaymentModes
{
    public class PaymentModePageViewModel : BaseViewModel
    {
        public PaymentModeSetupViewModel Setup { get; set; } = new PaymentModeSetupViewModel();
        public List<PaymentModeSetupViewModel> PaymentModeList { get; set; } = new List<PaymentModeSetupViewModel>();
        public DynamicGridModel? DynamicGrid { get; set; }
    }
}
