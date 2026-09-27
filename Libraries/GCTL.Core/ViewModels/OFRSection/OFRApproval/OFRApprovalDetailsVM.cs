
namespace GCTL.Core.ViewModels.OFRSection.OFRApproval
{
    public class OFRApprovalDetailsVM : BaseViewModel
    {
        public decimal? Tc { get; set; }
        public string? JobNo { get; set; }
        public string? OFFrNo { get; set; }
        public string CustomerNameID { get; set; }
        public string ShipmentModeID { get; set; }
        public string ReqNo { get; set; }
        public DateTime RequDate { get; set; }
        public string ServiceTypeID { get; set; }
        public string? AccountHeadID { get; set; }
        public decimal? Amount { get; set; }
        public string? Remark { get; set; }
        public string? EmployeeID { get; set; }
        public string? ServiceTypeName { get; set; }
        public string? AccountHeadName { get; set; }
        public string? IsReceivetable { get; set; }
        public string? SerialNo { get; set; }
        //
        public decimal? ApprovalAmount { get; set; }
        public string OFRDetailsID { get; set; }
        public string? BankAccount { get; set; }
        public string? CashBank { get; set; }

    }
}
