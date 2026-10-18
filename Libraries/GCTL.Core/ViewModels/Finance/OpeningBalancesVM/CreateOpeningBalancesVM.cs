using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.Finance.OpeningBalancesVM
{
    public class CreateOpeningBalancesVM : BaseViewModel
    {
        public int? OpeningBalanceID { get; set; }

        [Required(ErrorMessage = "{0} is Required!"), Display(Name = "Main Account")]
        public int? MainAccountID { get; set; }

        [Required(ErrorMessage = "{0} is Required!"), Display(Name = "Sub Account")]
        public int? SubAccountID { get; set; }

        [Required(ErrorMessage = "{0} is Required!"), Display(Name = "Transaction Account Name")]
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
