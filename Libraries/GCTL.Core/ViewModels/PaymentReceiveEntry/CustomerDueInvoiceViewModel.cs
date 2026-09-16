using System.Text.Json.Serialization;

namespace GCTL.Core.ViewModels.PaymentReceiveEntry
{
    public class CustomerDueInvoiceViewModel
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerAddress { get; set; }
        public string BillNo { get; set; } = string.Empty;
        public string? BillDate { get; set; }
        public decimal? BillAmount { get; set; }
        public decimal? Received { get; set; }
        public decimal? Due { get; set; }
        public decimal? TotalDue { get; set; }
    }
}
