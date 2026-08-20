using GCTL.Core.DataTables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.ShipmentModeLedgerSetup
{

    public class ShipmentModeLedgerSetupSetupViewModel : BaseViewModel
    {
        public int? Tc { get; set; }
        public string? ExpenseTypeId { get; set; }     
        public string? ExpenseType { get; set; }
        public string? ShortName { get; set; }
        public string? ExpenseLedgerCodeNo { get; set; }
        public string? RevenueLedgerCodeNo { get; set; }
        public string? AdvanceLedgerCode { get; set; }
        public string? TransportModeTypeId { get; set; }
    }
}
