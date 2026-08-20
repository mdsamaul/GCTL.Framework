using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels
{
    public class ShipmentModeVM:BaseViewModel
    {
        public int? TC { get; set; }
        [Required]
        public string ExpenseTypeID { get; set; }
        [Required]
        public string ExpenseType { get; set; }
        public string? transportModeTypeId { get; set; }
        public string? transportModeType { get; set; }

        public string? ShortName { get; set; }

        //public string? LUser { get; set; }

        //public DateTime? LDate { get; set; }

        //public string? LIP { get; set; }

        //public string? LMAC { get; set; }

        //public DateTime? ModifyDate { get; set; }

        public string? ExpenseLedgerCodeNo { get; set; }

        public string? RevenueLedgerCodeNo { get; set; }

        public string? AdvanceLedgerCode { get; set; }
    }
}
