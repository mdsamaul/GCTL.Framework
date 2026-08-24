using System;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.PaymentTerms
{
    public class PaymentTermsSetupViewModel : BaseViewModel
    {
        public int Tc { get; set; }
        public int? AutoId { get; set; }

        [Display(Name = "Payment Terms ID")]
        public string? PaymentTermsId { get; set; }

        [Display(Name = "Payment Terms Name")]
        public string? PaymentTermsName { get; set; }

        [Required(ErrorMessage = "Percentage is required.")]
        [Display(Name = "Percentage")]
        public string? Percentise { get; set; }

        [Required(ErrorMessage = "Type is required.")]
        [Display(Name = "Type")]
        public string? Type { get; set; }

        [Display(Name = "Credit Days")]
        public int? CreditDays { get; set; }

        [Display(Name = "Payment Type")]
        public string? PaymentType { get; set; }
    }
}
