namespace GCTL.Core.ViewModels.OFRSection.OFRBillAdjust
{
    public class OFRBillAdjustDetailsSaveVM : BaseViewModel
    {
        public decimal TC { get; set; }
        public string OFRNo { get; set; }
        public string OFR_DetailsID { get; set; }
        public decimal AdjustAmount { get; set; }
        public decimal DifferentAmount { get; set; }
        public decimal PortCharge { get; set; }
        public decimal AsycodaCharge { get; set; }
        public decimal DutyChalanCharge { get; set; }
        public decimal ShipingCharge { get; set; }
    }
}
