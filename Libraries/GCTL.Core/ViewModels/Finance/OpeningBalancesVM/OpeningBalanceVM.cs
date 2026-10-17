using System;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.Finance.OpeningBalancesVM
{
    public class OpeningBalanceVM
    {
        public decimal? AutoId { get; set; }

        [Required(ErrorMessage = "Opening Balance Code is required.")]
        [Display(Name = "OB Code")]
        public string ComOpeningBalanceCode { get; set; } = string.Empty;

        [Display(Name = "Company")]
        public int? Main_CompanyCode { get; set; }

        public string? CompanyName { get; set; }

        [Display(Name = "Branch")]
        public int? BranchCode { get; set; }

        public string? BranchName { get; set; }

        [Required(ErrorMessage = "Financial Year is required.")]
        [Display(Name = "Financial Year")]
        public string FinancialCodeNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account Head is required.")]
        [Display(Name = "Account Head")]
        public string GeneralLedgerCodeNo { get; set; } = string.Empty;

        public string? GeneralLedgerName { get; set; }

        [Required(ErrorMessage = "Opening Balance is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be a positive number.")]
        [Display(Name = "Opening Balance")]
        public decimal? OpeningBalance { get; set; }

        [Required(ErrorMessage = "Transaction Type is required.")]
        [Display(Name = "Tr. Type")]
        public string TrType { get; set; } = string.Empty;

        public string? AccountHeadDisplay => !string.IsNullOrEmpty(GeneralLedgerName) && !string.IsNullOrEmpty(GeneralLedgerCodeNo)
            ? $"{GeneralLedgerCodeNo} - {GeneralLedgerName}"
            : (GeneralLedgerName ?? GeneralLedgerCodeNo);

        [Display(Name = "OB Date")]
        public DateTime? OpeningDate { get; set; }

        public string? OpeningDateRaw { get; set; }

        public string? OpeningDateFormatted => OpeningDate?.ToString("dd/MM/yyyy");

        public string? OpeningDateIso => OpeningDate?.ToString("yyyy-MM-dd");

        [MaxLength(100, ErrorMessage = "Reference cannot exceed 100 characters.")]
        public string? Reference { get; set; }

        [MaxLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        public string? Description { get; set; }
    }
}
