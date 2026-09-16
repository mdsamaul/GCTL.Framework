using System.Text.Json.Serialization;

namespace GCTL.Core.ViewModels.PaymentReceiveEntry
{
    public class CustomerPaymentListViewModel
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public decimal? TotalSales { get; set; }
        public decimal? Due { get; set; }
        public decimal? LastSalesAmount { get; set; }
        public string? LastSalesDate { get; set; }
        public decimal? LastPaymentAmount { get; set; }
        public string? LastPaymentDate { get; set; }
    }
}
