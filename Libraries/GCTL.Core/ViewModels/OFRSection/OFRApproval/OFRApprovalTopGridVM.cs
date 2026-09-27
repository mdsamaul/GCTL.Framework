
using GCTL.Core.DataTables;

namespace GCTL.Core.ViewModels.OFRSection.OFRApproval
{
    public class OFRApprovalTopGridVM
    {
        public decimal TC { get; set; }
        [Searchable] public string JobNo { get; set; }
        [Searchable] public DateTime? JobDate { get; set; }
        [Searchable] public string? CustomerName { get; set; }
        public string CustomerID { get; set; }
        [Searchable] public string? CustomerAddress { get; set; }
        [Searchable] public DateTime? DocReceivedDate { get; set; }
        [Searchable] public string? Invoice { get; set; }
        [Searchable] public string? BENo { get; set; }
        [Searchable] public string? ShipmentMode { get; set; }
        public string? ShipmentModeID { get; set; }
    }
}
