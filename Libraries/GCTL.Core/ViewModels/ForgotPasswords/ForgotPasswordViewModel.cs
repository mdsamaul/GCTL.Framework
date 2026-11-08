using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.ForgotPasswords
{
    public class ForgotPasswordViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Reset Method")]
        public string ResetMethod { get; set; } = "otp"; // "secureCode" or "otp"

        [Display(Name = "Secure Code")]
        public string? SecureCode { get; set; }

        [Display(Name = "OTP")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be 6 digits")]
        public string? Otp { get; set; }

        [Required(ErrorMessage = "New Password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? LIP { get; set; }
        public string? LMAC { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.Equals(ResetMethod, "otp", System.StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(Otp))
                {
                    yield return new ValidationResult("OTP is required when resetting via OTP.", new[] { nameof(Otp) });
                }
            }
            else if (string.Equals(ResetMethod, "secureCode", System.StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(SecureCode))
                {
                    yield return new ValidationResult("Secure Code is required when resetting via Secure Code.", new[] { nameof(SecureCode) });
                }
            }
            else if (string.IsNullOrWhiteSpace(SecureCode) && string.IsNullOrWhiteSpace(Otp))
            {
                yield return new ValidationResult("Either OTP or Secure Code is required.", new[] { nameof(Otp), nameof(SecureCode) });
            }
        }
    }
}

