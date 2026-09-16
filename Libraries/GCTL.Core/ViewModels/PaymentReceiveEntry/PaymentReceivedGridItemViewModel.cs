using System.Text.Json.Serialization;

namespace GCTL.Core.ViewModels.PaymentReceiveEntry
{
    public class PaymentReceivedGridItemViewModel
    {
        [JsonPropertyName("paymentReceiveId")]
        public string PaymentReceiveId { get; set; } = string.Empty;

        [JsonPropertyName("paymentReceiveID")]
        public string PaymentReceiveID
        {
            get => PaymentReceiveId;
            set => PaymentReceiveId = value;
        }

        [JsonPropertyName("paymentReceiveDate")]
        public string? PaymentReceiveDate { get; set; }

        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        [JsonPropertyName("customerID")]
        public string CustomerID
        {
            get => CustomerId;
            set => CustomerId = value;
        }

        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("customerAddress")]
        public string? CustomerAddress { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("paymentType")]
        public string? PaymentType { get; set; }

        [JsonPropertyName("paymentModeName")]
        public string? PaymentModeName { get; set; }
    }
}
