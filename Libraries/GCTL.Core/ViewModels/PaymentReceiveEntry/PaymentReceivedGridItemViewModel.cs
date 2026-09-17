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
        public string? PaymentModeName { get; set; }
    }
}
