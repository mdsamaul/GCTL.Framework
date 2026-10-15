using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.Finance.OpeningBalancesVM
{
    public class UpdateOpeningBalancesVM : BaseViewModel
    {
        public int? OpeningBalanceID { get; set; }

        [Required(ErrorMessage = "{0} is Required!"), Display(Name = "Transaction Account")]
        public int? TrxAccID { get; set; }

        [Required(ErrorMessage = "{0} is Required!"), Display(Name = "Opening Balance Code")]
        public string OpeningBalanceCode { get; set; }

        [Required(ErrorMessage = "{0} is Required!"), Display(Name = "Amount")]
        public decimal? Amount { get; set; }

        [Required(ErrorMessage = "{0} is Required!"), Display(Name = "Transaction Type")]
        public string TrxType { get; set; }

        public string? Description { get; set; }
    }
}
