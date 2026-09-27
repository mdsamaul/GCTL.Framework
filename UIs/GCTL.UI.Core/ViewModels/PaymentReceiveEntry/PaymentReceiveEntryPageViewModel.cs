using System.Collections.Generic;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.PaymentReceiveEntry;

namespace GCTL.UI.Core.ViewModels.PaymentReceiveEntry
{
    public class PaymentReceiveEntryPageViewModel : BaseViewModel
    {
        public PaymentReceiveEntrySetupViewModel Setup { get; set; } = new PaymentReceiveEntrySetupViewModel();
        public List<PaymentReceiveEntrySetupViewModel> PaymentReceiveList { get; set; } = new List<PaymentReceiveEntrySetupViewModel>();
        public List<CustomerPaymentListViewModel> CustomerList { get; set; } = new List<CustomerPaymentListViewModel>();
        public TabGroupModel TabModel { get; set; }
    }
}
