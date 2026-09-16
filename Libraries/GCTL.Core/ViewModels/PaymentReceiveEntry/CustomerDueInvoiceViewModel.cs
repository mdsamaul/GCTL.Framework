using System.Text.Json.Serialization;

namespace GCTL.Core.ViewModels.PaymentReceiveEntry
{
    public class CustomerDueInvoiceViewModel
    {
        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        [JsonPropertyName("customerID")]
        public string CustomerID
        {
            get => CustomerId;
            set => CustomerId = value;
        }

        [JsonPropertyName("customerAddress")]
        public string? CustomerAddress { get; set; }

        [JsonPropertyName("billNo")]
        public string BillNo { get; set; } = string.Empty;

        [JsonPropertyName("billDate")]
        public string? BillDate { get; set; }

        [JsonPropertyName("billAmount")]
        public decimal? BillAmount { get; set; }

        [JsonPropertyName("received")]
        public decimal? Received { get; set; }

        [JsonPropertyName("due")]
        public decimal? Due { get; set; }

        [JsonPropertyName("totalDue")]
        public decimal? TotalDue { get; set; }
    }
}
