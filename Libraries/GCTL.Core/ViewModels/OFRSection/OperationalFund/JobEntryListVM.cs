
using GCTL.Core.DataTables;

namespace GCTL.Core.ViewModels.OFRSection.OperationalFund
{
    public class JobEntryListVM
    {
        [Searchable]public string JobNo { get; set; }
        [Searchable]public DateTime? JobDate { get; set; }
        [Searchable]public string Customer { get; set; }
         public string CustomerID { get; set; }
        [Searchable] public string CustomerAddress { get; set; }

        [Searchable] public DateTime? DocReceivedDate { get; set; }
        [Searchable] public string? Invoice { get; set; }
        [Searchable] public string? Beno { get; set; }
        [Searchable] public string? ShipmentMode { get; set; }
        public string? ShipmentModeID { get; set; }
    }
}
