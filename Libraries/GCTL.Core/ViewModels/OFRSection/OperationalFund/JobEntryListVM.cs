
namespace GCTL.Core.ViewModels.OFRSection.OperationalFund
{
    public class JobEntryListVM 
    {
        public string JobNo { get; set; }
        public DateTime? JobDate { get; set; }
        public string Customer { get; set; }
        public string CustomerID { get; set; }

        public DateTime? DocReceivedDate { get; set; }
        public string? ShipmentMode { get; set; }
        public string? ShipmentModeID { get; set; }


    }
}
