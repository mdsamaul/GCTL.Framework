

namespace GCTL.Core.ViewModels.ChartOfAccount
{
    public class TreeLedgerReportVM
    {
        public string CodeNo { get; set; }
        public string Name { get; set; }
        public string ParentCodeNo { get; set; }
        public string ParentName { get; set; }
        public List<TreeLedgerReportVM> Children { get; set; } = new List<TreeLedgerReportVM>();

    }
}
