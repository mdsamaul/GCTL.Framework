

namespace GCTL.Core.ViewModels.ChartOfAccount
{
    public class AccountReportVM
    {
        public string GroupLedger { get; set; }
        public string ControlLedger { get; set; }
        public string SubControlLedger { get; set; }
        public string SubSidiaryLedger { get; set; }
        public string AccountCode { get; set; }
        public string GeneralLedger { get; set; }
        public int Level { get; set; }
    }

}
