
using GCTL.Core.DataTables;

namespace GCTL.Core.ViewModels.OFRSection.OperationalFund
{
    public class RequisitionVM
    {
        public decimal TC { get; set; }
        [Searchable] public string OFRNo { get; set; }
        [Searchable] public DateTime? OFRDate { get; set; }
        [Searchable] public string JobNo { get; set; }
        [Searchable] public string? CustomerName { get; set; }
        [Searchable] public string? ShipmentMode { get; set; }
        [Searchable] public string? BillingAddress { get; set; }

    }
}
