using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.Finance.OpeningBalancesVM
{
    public class GetAllOpeningBalancesVM
    {
        public int? OpeningBalanceID { get; set; }

        public string? TrxAccName { get; set; }

        public string? OpeningBalanceCode { get; set; }

        public decimal? Amount { get; set; }

        public string? TrxType { get; set; }

        public string? Description { get; set; }
    }
}
