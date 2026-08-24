using System;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.CurrencyType
{
    public class CurrencyTypeSetupViewModel : BaseViewModel
    {
        public int Tc { get; set; }
        public decimal AutoId { get; set; }

        public string? CurrencyId { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Currency Name")]
        public string CurrencyName { get; set; } = string.Empty;

        [Display(Name = "Short Name")]
        public string? ShortName { get; set; }

        [Display(Name = "Symbol")]
        public string? Symbol { get; set; }

        [Display(Name = "Decimal Places")]
        public int? DecimalPlaces { get; set; }

        [Display(Name = "Negative Format")]
        public string? NegativeFormat { get; set; }
    }
}
