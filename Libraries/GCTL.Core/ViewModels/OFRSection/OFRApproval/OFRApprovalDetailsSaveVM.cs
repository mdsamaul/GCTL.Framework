

namespace GCTL.Core.ViewModels.OFRSection.OFRApproval
{
    public class OFRApprovalDetailsSaveVM : BaseViewModel
    {
        public decimal TC { get; set; }
        public decimal ActualAmount { get; set; }
        public string OFRNo { get; set; }
        public string OFR_DetailsID { get; set; }
        public string DetailsCashBank { get; set; }
        public string DetailsBankAccount { get; set; }
    }
}
