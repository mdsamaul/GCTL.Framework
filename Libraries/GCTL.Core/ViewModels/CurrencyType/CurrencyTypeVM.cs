using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.CurrencyType
{
    public class CurrencyTypeVM
    {
        public decimal? TC { get; set; }

        public string? CurrencyId { get; set; }

        public string CurrencyName { get; set; }

        public string? ShortName { get; set; }

        public string? Symbol { get; set; }

        public int? DecimalPlaces { get; set; }

        public string? NegativeFormat { get; set; }

        public string? LUser { get; set; }

        public DateTime? LDate { get; set; }

        public string? LIP { get; set; }

        public string? LMAC { get; set; }

        public DateTime? ModifyDate { get; set; }
    }
}
