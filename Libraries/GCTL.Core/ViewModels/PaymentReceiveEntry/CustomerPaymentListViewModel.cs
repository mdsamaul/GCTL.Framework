using System.Text.Json.Serialization;

namespace GCTL.Core.ViewModels.PaymentReceiveEntry
{
    public class CustomerPaymentListViewModel
    {
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

        [JsonPropertyName("totalSales")]
        public decimal? TotalSales { get; set; }

        [JsonPropertyName("due")]
        public decimal? Due { get; set; }

        [JsonPropertyName("lastSalesAmount")]
        public decimal? LastSalesAmount { get; set; }

        [JsonPropertyName("lastSalesDate")]
        public string? LastSalesDate { get; set; }

        [JsonPropertyName("lastPaymentAmount")]
        public decimal? LastPaymentAmount { get; set; }

        [JsonPropertyName("lastPaymentDate")]
        public string? LastPaymentDate { get; set; }
    }
}
