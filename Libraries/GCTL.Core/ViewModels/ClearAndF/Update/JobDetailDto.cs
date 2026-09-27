
namespace GCTL.Core.ViewModels.ClearAndF.Update
{
    public class JobDetailDto
    {
        public int Id { get; set; } // Primary key
        public string JobNo { get; set; }
        public string ShipmentMode { get; set; }
        public string? ShipmentModeId { get; set; }
        public string IsCustomJobNo { get; set; }
        public string JobDate { get; set; } // "yyyy-MM-dd"
        public string? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerDeliveryAddress { get; set; }
        public string? PortId { get; set; }
        public string DocsReceivedDate { get; set; }
        public string LcExpNo { get; set; }
        public decimal? LcValue { get; set; }
        public string IpEpNo { get; set; }
        public string IpDate { get; set; }
        public string? ImporterId { get; set; }
        public string InvoiceNo { get; set; }
        public string InvoiceDate { get; set; }
        public string BlNo { get; set; }
        public string BlDate { get; set; }
        public string BeNo { get; set; }
        public string BeDate { get; set; }
        public string ContainerNo { get; set; }
        public string ContainerSize { get; set; }
        public string LcaNo { get; set; }
        public string DischargeDate { get; set; }
        public string MaterialDescription { get; set; }
        public decimal? Quantity { get; set; }
        public string? QuantityUnitId { get; set; }
        public decimal? Weight { get; set; }
        public string? WeightUnitId { get; set; }
        public string? ForwarderId { get; set; }
        public string VesselRottNo { get; set; }
        public string Remarks { get; set; }
        public string DocReceivedDate { get; set; } // matches table column
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}