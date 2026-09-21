using System.Text.Json.Serialization;

namespace GCTL.Core.ViewModels.PaymentReceiveEntry
{
    public class PaymentReceivedGridItemViewModel
    {
        public string PaymentReceiveId { get; set; } = string.Empty;
        public string? PaymentReceiveDate { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public decimal? Amount { get; set; }
        public string? PaymentType { get; set; }
        public string? ReceiveVia { get; set; }
        public string? PaymentModeName { get; set; }
        public decimal? BankCharge { get; set; }
        public string? BillList { get; set; }
        public string? Receivedby { get; set; }
        public string? Status { get; set; }
        public decimal? AIT { get; set; }
        public string? AITChallanNo { get; set; }
        public string? AITInvoiceNo { get; set; }
        public string? AITChallanUpload { get; set; }
    }
}
