using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCTL.Core.DataTables;

namespace GCTL.Core.ViewModels.ExpenseHead
{
    public class ExpenseHeadVM : BaseViewModel
    {
        public int? TC { get; set; }

        [Searchable]
        public string ExpenseHeadID { get; set; }

        [Searchable]
        public string ExpenseHead { get; set; }

        [Searchable]
        public string? ShortName { get; set; }

        [Searchable]
        public string SerialNo { get; set; }

        public string ExpenseTypeID { get; set; }

        [Searchable]
        public string? IsReceiptable { get; set; }

        public decimal? Amount { get; set; }

        public string ServiceTypeID { get; set; }

        public string? EntryType { get; set; }

        [Searchable]
        public string? ExpenseType { get; set; }

        [Searchable]
        public string? ServiceTypeName { get; set; }
    }
}

