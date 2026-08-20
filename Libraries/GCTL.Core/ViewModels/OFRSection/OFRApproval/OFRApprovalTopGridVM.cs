
using GCTL.Core.DataTables;

namespace GCTL.Core.ViewModels.OFRSection.OFRApproval
{
    public class OFRApprovalTopGridVM
    {
        public decimal TC { get; set; }
        //public string OFRNo { get; set; }
        //public DateTime? OFRDate { get; set; }
        [Searchable] public string JobNo { get; set; }
        [Searchable] public string? CustomerName { get; set; }
        [Searchable] public string? ShipmentMode { get; set; }
        //public string? BillingAddress { get; set; }
        public string CustomerID { get; set; }
        public string? ShipmentModeID { get; set; }


    }
}
