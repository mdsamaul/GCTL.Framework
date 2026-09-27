using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.BankInformations
{
    public class BankInformationsSetupViewModel : BaseViewModel
    {
        public int Tc { get; set; }
        public decimal AutoId { get; set; }
        public string? BankId { get; set; }
        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Bank Name")]
        public string BankName { get; set; }
        [Display(Name = "Short Name")]
        public string? ShortName { get; set; }
    }
}
