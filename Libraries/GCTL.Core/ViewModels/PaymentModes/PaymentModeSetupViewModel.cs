using System;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.PaymentModes
{
    public class PaymentModeSetupViewModel : BaseViewModel
    {
        public int Tc { get; set; }
        public int? AutoId { get; set; }
        [Display(Name = "Payment Mode ID")]
        public string? PaymentModeId { get; set; }
        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Payment Mode Name")]
        public string PaymentModeName { get; set; } = string.Empty;
        [Display(Name = "Short Name")]
        public string? PaymentModeShortName { get; set; }
    }
}
