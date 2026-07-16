
namespace GCTL.Core.ViewModels.OFRSection.OperationalFund
{
    public class RequsitionMasterVM :BaseViewModel
    {
        public string JobNo { get; set; }
        public string OFFRNo { get; set; }
        public DateTime OFFRDate { get; set; }
        public string ExpenseTypeID { get; set; }
        public string? ServiceTypeID { get; set; }
        public string? CustomerID { get; set; }
        //Details
        public List<RequistionDetailsTmpVM> Details { get; set; } = new();

    }
}
