using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.Finance.VoucherEntryVM
{
    public class VoucherEntryVM
    {
        public decimal? AutoId { get; set; }

        public string VoucherEntryCodeNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Voucher No is required.")]
        [Display(Name = "Voucher No")]
        public string VoucherNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Voucher Type is required.")]
        [Display(Name = "Voucher Type")]
        public string VoucherType_Code { get; set; } = string.Empty;

        public string? VoucherTypeName { get; set; }

        [Required(ErrorMessage = "Voucher Date is required.")]
        [Display(Name = "Voucher Date")]
        public DateTime VoucherDate { get; set; } = DateTime.Today;

        public string VoucherDateFormatted => VoucherDate.ToString("dd/MM/yyyy");

        public string VoucherDateIso => VoucherDate.ToString("yyyy-MM-dd");

        public string? CompanyCode { get; set; }

        public string? Main_CompanyCode { get; set; }

        public string? CompanyName { get; set; }
        public string? CompanyAddress { get; set; }

        public string? BranchCode { get; set; }

        public string? BranchName { get; set; }

        public string? FinancialYear { get; set; }

        public string? PeriodCodeNo { get; set; }

        public string? CostCenterCodeNo { get; set; }

        [MaxLength(50, ErrorMessage = "Reference cannot exceed 50 characters.")]
        [Display(Name = "Reference / Invoice No")]
        public string? Reference { get; set; }

        [MaxLength(50, ErrorMessage = "Invoice No cannot exceed 50 characters.")]
        public string? InvoiceNo { get; set; }

        [Required(ErrorMessage = "Narration is required.")]
        [MaxLength(500, ErrorMessage = "Narration cannot exceed 500 characters.")]
        [Display(Name = "Narration")]
        public string? Narration { get; set; }

        [Display(Name = "Status")]
        public string IsApproved { get; set; } = "Pending Approval"; // "Pending Approval" or "Posted"

        public string? PreparedBy { get; set; }
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public decimal TotalDebit { get; set; }

        public decimal TotalCredit { get; set; }

        public decimal TotalAmount => TotalDebit > 0 ? TotalDebit : TotalCredit;

        public string? AmountInWords { get; set; }

        public List<VoucherEntryDetailVM> Details { get; set; } = new List<VoucherEntryDetailVM>();
    }
}
