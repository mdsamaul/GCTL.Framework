using System;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.Finance.VoucherEntryVM
{
    public class VoucherEntryDetailVM
    {
        public decimal? AutoId { get; set; }

        public string? VoucherEntryDetailsCodeNo { get; set; }

        public string? VoucherEntryCodeNo { get; set; }

        [Required(ErrorMessage = "Account Head is required.")]
        [Display(Name = "Account Head")]
        public string AccCode { get; set; } = string.Empty;

        public string? AccName { get; set; }

        public string? AccountPath { get; set; }

        [Required(ErrorMessage = "Transaction Type is required.")]
        public string TrType { get; set; } = "Dr"; // "Dr" or "Cr"

        [MaxLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Debit Amount must be positive.")]
        public decimal? DebitAmount { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Credit Amount must be positive.")]
        public decimal? CreditAmount { get; set; } = 0;

        [MaxLength(100, ErrorMessage = "Cheque No cannot exceed 100 characters.")]
        public string? ChequeNo { get; set; }

        public DateTime? ChequeDate { get; set; }

        public string? ChequeDateFormatted => ChequeDate?.ToString("dd/MM/yyyy");

        public string? ChequeDateIso => ChequeDate?.ToString("yyyy-MM-dd");

        [MaxLength(100, ErrorMessage = "Bank Name cannot exceed 100 characters.")]
        public string? BankName { get; set; }

        [MaxLength(100, ErrorMessage = "Branch cannot exceed 100 characters.")]
        public string? Branch { get; set; }

        public bool IsCash { get; set; }
        public bool IsBank { get; set; }
    }
}
