using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.Currencies
{
    public class CurrencyVM : BaseViewModel
    {
        public int CurrencyID { get; set; }

        public string? CurrencyCode { get; set; }

        public string CurrencyName { get; set; }

        public string? Symbol { get; set; }
    }
}
