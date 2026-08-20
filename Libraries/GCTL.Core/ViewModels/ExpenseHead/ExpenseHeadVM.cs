using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.ExpenseHead
{
    public class ExpenseHeadVM
    {
        public int? TC { get; set; }

        public string ExpenseHeadID { get; set; }

        public string ExpenseHead { get; set; }

        public string? ShortName { get; set; }

        public string? LUser { get; set; }

        public DateTime? LDate { get; set; }

        public string? LIP { get; set; }

        public string? LMAC { get; set; }

        public DateTime? ModifyDate { get; set; }

        public string SerialNo { get; set; }

        public string ExpenseTypeID { get; set; }

        public string? IsReceiptable { get; set; }

        public decimal? Amount { get; set; }

        public string ServiceTypeID { get; set; }

        public string? EntryType { get; set; }
        public string? ExpenseType { get; set; }
        public string? ServiceTypeName { get; set; }
    }
}
