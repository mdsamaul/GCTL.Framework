using System;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.PaymentType
{
    public class PaymentTypeSetupViewModel : BaseViewModel
    {
        public int Tc { get; set; }
        public int? AutoId { get; set; }

        [Display(Name = "Payment Type ID")]
        public string? PaymentTypeId { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Payment Type")]
        public string PaymentType { get; set; } = string.Empty;

        [Display(Name = "Short Name")]
        public string? ShortName { get; set; }
    }
}
