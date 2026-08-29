using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCTL.Core.DataTables;

namespace GCTL.Core.ViewModels
{
    public class ShipmentModeVM:BaseViewModel
    {
        public int? TC { get; set; }
        [Required]
        [Searchable]
        public string ExpenseTypeID { get; set; }
        [Required]
        [Searchable]
        public string ExpenseType { get; set; }
        public string? transportModeTypeId { get; set; }
        [Searchable]
        public string? transportModeType { get; set; }

        [Searchable]
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
