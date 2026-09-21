using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.PaymentReceiveEntry
{
    public class PaymentReceiveEntrySetupViewModel : BaseViewModel
    {
        public decimal? Tc { get; set; }

        [Required(ErrorMessage = "Payment Receive No. is required")]
        [Display(Name = "Payment Receive No.")]
        public string PaymentReceiveId { get; set; } = string.Empty;

        [Display(Name = "Payment Receive Date")]
        public DateTime? PaymentReceiveDate { get; set; } = DateTime.Now;

        // Backward compatibility
        public DateTime? ReceiveDate
        {
            get => PaymentReceiveDate;
            set => PaymentReceiveDate = value;
        }

        [Required(ErrorMessage = "Customer is required")]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        public string? CustomerName { get; set; }

        [Display(Name = "Billing Address")]
        public string? BillingAddress { get; set; }

        [Display(Name = "Against/Bill List")]
        public string? AgainstBillList { get; set; }

        [Display(Name = "Payment Type")]
        public string? PaymentTypeId { get; set; } = "01";

        public string? PaymentTypeName { get; set; }

        [Required(ErrorMessage = "Payment Mode is required")]
        [Display(Name = "Payment Mode")]
        public string PaymentModeCode { get; set; } = "001";

        public string? PaymentModeName { get; set; }

        [Display(Name = "Money Receipt No.")]
        public string? MrNo { get; set; }

        [Display(Name = "Money Receipt Date")]
        public DateTime? MrDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Payment Amount is required")]
        [Display(Name = "Payment Amount")]
        public decimal? Amount { get; set; }

        // Backward compatibility
        public decimal? ReceivedAmount
        {
            get => Amount;
            set => Amount = value;
        }

        [Display(Name = "Bank Fee/Online Charge")]
        public decimal? BankFeeOnlineCharge { get; set; }

        [Display(Name = "AIT")]
        public decimal? AIT { get; set; }

        [Display(Name = "AIT Challan No")]
        public string? AITChallanNo { get; set; }

        [Display(Name = "AIT Invoice No.")]
        public string? AITInvoiceNo { get; set; }

        [Display(Name = "AIT Challan Upload")]
        public string? AITChallanUpload { get; set; }

        public Microsoft.AspNetCore.Http.IFormFile? AITChallanFile { get; set; }

        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [Display(Name = "Received by")]
        public string? Receivedby { get; set; }

        public string? ReceivedbyName { get; set; }

        public string? CreationDate { get; set; }

        public string? LastUpdateDate { get; set; }

        public string? SelectedInvoicesJson { get; set; }

        public List<PaymentReceiveInvoiceItem>? InvoiceItems { get; set; } = new List<PaymentReceiveInvoiceItem>();

        // Additional fields for compatibility
        public string? JobNo { get; set; }
        public string? BillNo { get; set; }
        public string? BankId { get; set; }
        public string? BankName { get; set; }
        public string? BranchName { get; set; }
        public string? ChequeNo { get; set; }
        public DateTime? ChequeDate { get; set; }
        public decimal? BillAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DueAmount { get; set; }
        public string? CompanyCode { get; set; }
        public string? EmployeeId { get; set; }
    }
}
