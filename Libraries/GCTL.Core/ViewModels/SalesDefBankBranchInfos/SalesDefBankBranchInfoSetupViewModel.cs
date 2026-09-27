using System;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.SalesDefBankBranchInfos
{
    public class SalesDefBankBranchInfoSetupViewModel : BaseViewModel
    {
        public int Tc { get; set; }
        public int? AutoId { get; set; }
        public string? BankBranchId { get; set; }
        [Required(ErrorMessage = "Select Bank Name.")]
        [Display(Name = "Bank Name")]
        public string BankId { get; set; } = string.Empty;
        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }
        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Branch Name")]
        public string BankBranchName { get; set; } = string.Empty;
        [Display(Name = "Short Name")]
        public string? ShortName { get; set; }
        [Display(Name = "Swift Code")]
        public string? Swiftcode { get; set; }
        [Display(Name = "Address")]
        public string? Address { get; set; }
        [Display(Name = "Phone")]
        public string? Phone { get; set; }
    }
}
