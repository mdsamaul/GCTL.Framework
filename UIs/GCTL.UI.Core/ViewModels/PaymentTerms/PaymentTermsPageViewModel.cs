using System.Collections.Generic;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.PaymentTerms;

namespace GCTL.UI.Core.ViewModels.PaymentTerms
{
    public class PaymentTermsPageViewModel : BaseViewModel
    {
        public PaymentTermsSetupViewModel Setup { get; set; } = new PaymentTermsSetupViewModel();
        public List<PaymentTermsSetupViewModel> PaymentTermsList { get; set; } = new List<PaymentTermsSetupViewModel>();
        public DynamicGridModel? DynamicGrid { get; set; }
    }
}