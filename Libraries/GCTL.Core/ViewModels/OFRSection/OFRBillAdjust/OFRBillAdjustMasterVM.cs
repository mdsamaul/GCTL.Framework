namespace GCTL.Core.ViewModels.OFRSection.OFRBillAdjust
{
    public class OFRBillAdjustMasterVM
    {
        public string OFRNo { get; set; }
        public string JobNo { get; set; }
        public DateTime? OFRDate { get; set; }
        public DateTime? BillAdjustModifiedDate { get; set; }
        public DateTime? BillAdjustApprovedDate { get; set; }
        public string CustomerName { get; set; }
        public string InvoiceNo { get; set; }
        public string HAWB { get; set; }
        public decimal? Quantity { get; set; }
        public string ShipmentMode { get; set; }
        public decimal? LCValue { get; set; }
        public string MaterialDescription { get; set; }
        public decimal? Weight { get; set; }
        public string CustomerID { get; set; }
        public string ShipmentModeID { get; set; }
        public decimal? PortCharge { get; set; }
        public decimal? AsycodaCharge { get; set; }
        public decimal? DutyChalanCharge { get; set; }
        public decimal? ShipingCharge { get; set; }
    }
}
