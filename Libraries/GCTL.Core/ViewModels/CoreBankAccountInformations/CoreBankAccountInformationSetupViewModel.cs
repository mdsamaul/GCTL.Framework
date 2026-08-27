using System;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.CoreBankAccountInformations
{
    public class CoreBankAccountInformationSetupViewModel : BaseViewModel
    {
        public int Tc { get; set; }
        public decimal? AutoId { get; set; }
        public string? AccInfoId { get; set; }
        [Required(ErrorMessage = "Select Bank.")]
        [Display(Name = "Bank Name")]
        public string? BankId { get; set; }
        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }
        [Required(ErrorMessage = "Select Branch.")]
        [Display(Name = "Branch Name")]
        public string? BranchId { get; set; }
        [Display(Name = "Branch Name")]
        public string? BranchName { get; set; }
        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Account Name")]
        public string AccountName { get; set; } = string.Empty;
        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Account No.")]
        public string AccountNo { get; set; } = string.Empty;
        public string? UserInfoEmployeeID { get; set; }
        public string? CompanyCode { get; set; }
    }
}
