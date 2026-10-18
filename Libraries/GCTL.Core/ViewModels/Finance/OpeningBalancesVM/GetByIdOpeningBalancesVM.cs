using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.Finance.OpeningBalancesVM
{
    public class GetByIdOpeningBalancesVM
    {
        public int? OpeningBalanceID { get; set; }

        public int? MainAccountID { get; set; }

        public int? SubAccountID { get; set; }

        public int? TrxAccID { get; set; }

        public string? OpeningBalanceCode { get; set; }

        public decimal? Amount { get; set; }

        public string? TrxType { get; set; }

        public string? Description { get; set; }
    }
}
