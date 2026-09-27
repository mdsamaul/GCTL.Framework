using System;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.UnitType
{
    public class UnitTypeSetupViewModel : BaseViewModel
    {
        public int Tc { get; set; }
        public int? AutoId { get; set; }
        public string? UnitTypID { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Unit Type Name")]
        public string UnitTypeName { get; set; } = string.Empty;

        [Display(Name = "Short Name")]
        public string? ShortName { get; set; }

        [Display(Name = "Decimal Places")]
        public int? DecimalPlaces { get; set; }
    }
}
