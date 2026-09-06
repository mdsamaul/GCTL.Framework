using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.ClearAndF.SepDocumentation
{
    public class JobEntryViewModel : BaseViewModel
    {
        public decimal? tc { get; set; }
        public string ShipmentMode { get; set; }
        public bool  IsCustomer { get; set; }
        public string CustomerName { get; set; }
        public string JobNo { get; set; }
        public string? CustomerAddress { get; set; }
        public DateTime? JobDate { get; set; }
        public string CustomerDeliveryAddress { get; set; }
        public string PortName { get; set; }
        public DateTime? DocsReceivedDate { get; set; }
        public string? ExpNo { get; set; }
        public DateTime? ExpDate { get; set; }
        public string? LCNo { get; set; }
        public DateTime? LCDate { get; set; }
        public string? LCValue { get; set; }
        public string? LCValue2 { get; set; }
        public string? CurrencyForLC { get; set; }
        public string? BDT { get; set; }
 //       public string? LcValue { get; set; }
        public string? IPEPNo { get; set; }
        public DateTime? IPDate { get; set; }
        public string? ImporterName { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? InvoiceNo { get; set; }
        public string? InvoiceValue { get; set; }
        public string? CurrencyForInvoice { get; set; }
        public string? AssessableRate { get; set; }
        public string? AssessableValue { get; set; }
        public string? BLNo { get; set; }
        public DateTime? BLDate { get; set; }
        public string? BENo { get; set; }
        public DateTime? BEDate { get; set; }
        public string? ContainerNo { get; set; }
        public string? ContainerSize { get; set; }
        public string? MaterialDescription { get; set; }
        public string? Quantity { get; set; }
        public string? QuantityUnit { get; set; }
        public string? Weight { get; set; }
        public string? WeightUnit { get; set; }
        public string? FWDRShippingAgent { get; set; }
        public DateTime? DischargeDate { get; set; }
        public DateTime? ETADeliveryDate { get; set; }
        public DateTime? ETACTGPORT { get; set; }
        public DateTime? UnstuffingDate { get; set; }
        public DateTime? ETDDate { get; set; }
        public string? VesselRotNo { get; set; }
        public DateTime? ActualDeliveryDT { get; set; }
        public DateTime? ShipArrivedDate { get; set; }
        public string? PlaceOfLoading { get; set; }
        public string? ShedYard { get; set; }
        public string? HAWB { get; set; }
        public string? MAWB { get; set; }
        public string? NameOfFreightForwarder { get; set; }
        public string? AssessmentNo { get; set; }
        public string? RNo { get; set; }
        public string? ClientStationWiseSLNo { get; set; }
        public string? FreightCharge { get; set; }
        public string? ShipmentStatus { get; set; }
        public string? Remarks { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTime? LastUpdateDate { get; set; }
    }
}
