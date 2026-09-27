using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.SepDocumentation
{
    /// <summary>
    /// Top-level page model for the Documentation/Job Entry + Document Upload Info screen.
    /// </summary>
    public class SepDocumentationSetupViewModel
    {
        public string PageTitle { get; set; }
        public string Breadcrumb { get; set; }
        public string PageUrl { get; set; }
    }

    /// <summary>
    /// Full entry/edit model - posted from the "Documentation/Job Entry" tab.
    /// Mirrors columns of dbo.Sep_Documentation.
    /// </summary>
    public class SepDocumentationEntryViewModel :BaseViewModel
    {
        public decimal? Tc { get; set; }               // 0 = new
        public string? JobNo { get; set; }
        public string? PrifixId { get; set; }
        public string? IsCustomJobNo { get; set; }
        public string? ShipmentModeId { get; set; }     // ExpenseTypeID (drives Sea/Air group)
        public string? TransportModeTypeId { get; set; } // "SEA" / "AIR" - resolved server-side too
        public DateTime? Date { get; set; }             // Job Date
        public string? CustomerId { get; set; }
        public string? CustomerAddress { get; set; }
        public string? DeliveryLocationCode { get; set; } // Customer Delivery Address
        public string? PortId { get; set; }
        public string? PortNameId { get; set; }
        public DateTime? DocReceivedDate { get; set; }

        public string? ExpNo { get; set; }
        public DateTime? ExpDate { get; set; }
        public string? LCNo { get; set; }
        public DateTime? LCDate { get; set; }
        public decimal? LCValue { get; set; }
        public string? CurrencyId { get; set; }
        public decimal? BDTRate { get; set; }
        public string? IPNo { get; set; }
        public DateTime? IPDate { get; set; }
        public string? ImporterID { get; set; }
        public string? ExporterID { get; set; }
        public string? InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public decimal? InvoiceValue { get; set; }
        public string? InvoiceCurrencyID { get; set; }
        public decimal? AssessableRate { get; set; }
        public decimal? AssessableValue { get; set; }
        public string? BLNo { get; set; }
        public DateTime? BLDate { get; set; }
        public string? ContainerNo { get; set; }
        public string? ContainerSize { get; set; }
        public string? MaterialDescription { get; set; }
        public decimal? Quntity1 { get; set; }
        public string? Unit1 { get; set; }
        public decimal? Quntity2 { get; set; }
        public string? Unit2 { get; set; }
        public string? FWDR_ShippingAgent { get; set; }
        public string? NameOfFreightForwarder { get; set; }
        public string? PhoneNo { get; set; }
        public string? VesselRottNo { get; set; }
        public string? VesselName { get; set; }
        public string? PlaceOfLoadingID { get; set; }
        public string? ShedYardID { get; set; }
        public string? HAWB { get; set; }
        public string? MAWB { get; set; }
        public string? AssessmentNo { get; set; }
        public string? RNo { get; set; }
        public string? ClientStationwiseSLNo { get; set; }
        public string? FreightCharge { get; set; }
        public string? LCANo { get; set; }
        public string? Term { get; set; }
        public string? Remarks { get; set; }

        // ---------- Shared Sea + Air delivery-tracking fields ----------
        public DateTime? ETDDate { get; set; }
        public DateTime? ETACTGPORT { get; set; }
        public DateTime? DocsBankSignDate { get; set; }
        public string? BENo { get; set; }
        public DateTime? BEDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }   // -> ATA

        // ---------- Sea-only fields ----------
        public DateTime? FinalIgmRecDate { get; set; }
        public DateTime? ShipArrivedDate { get; set; }      // -> SHIPARRIVEDDATE
        public DateTime? UnstuffDischargeDate { get; set; } // -> Un_StuffingLocationDate
        public DateTime? EstimatedDeliveryDate { get; set; } // -> ETA

        // ---------- Air-only fields ----------
        public DateTime? FlightArrivalDate { get; set; }    // -> ATB (reused)
        public DateTime? ArrivalNoticeDate { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DocumentHandoverDate { get; set; }
        public DateTime? CargoHandoverDate { get; set; }
        public string? UndertakingNo { get; set; }
        public string? PONo { get; set; }
        public string? ShipmentStatus { get; set; }
        public string? SupplierId { get; set; }
        public string? VehicleId { get; set; }
        public string? Vehicle { get; set; }
        public string? Vendor { get; set; }
        public string? DocumentationId { get; set; }
        public string? ConsoleNo { get; set; }
        public string? DocumentTypeId { get; set; }
        public string? CustomerID_Importer { get; set; }
        public string? DepotID { get; set; }
        public string? BookingConfirmationReference { get; set; }
        public string? PackingUnitType { get; set; }
        public decimal? TransportCost { get; set; }
        public bool? IsEdit { get; set; }
        public decimal? MinimumAgencyCom { get; set; }
        public decimal? CommOnInvoiceValue { get; set; }
        public decimal? PackingQuantity { get; set; }
    }

    public class SepDocJobListDto
    {
        public decimal Tc { get; set; }
        public string JobNo { get; set; }
        public DateTime? JobDate { get; set; }
        public string JobDateFormatted { get; set; }
        public string CustomerId { get; set; } = "";
        public string ShipmentModeId { get; set; } = "";
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string ShipmentMode { get; set; }
        public DateTime? DocReceivedDate { get; set; }
        public string DocReceivedDateFormatted { get; set; }
        public decimal? LCValue { get; set; }
        public string JobStatus { get; set; }

    }

    public class SepDocPagedResult<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int TotalRecords { get; set; }
        public int FilteredRecords { get; set; }
    }

    public class SepDocDeleteRequest
    {
        public List<decimal> Ids { get; set; }
    }
}