namespace GCTL.Core.ViewModels.PaymentReceiveEntry
{
    public class PaymentHistorySummaryViewModel
    {
        public string CustomerID { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? NetServiceValue { get; set; }
        public string? NetReceivedAmount { get; set; }
        public string? TotalDue { get; set; }
        public string? OverDue { get; set; }
    }
}
