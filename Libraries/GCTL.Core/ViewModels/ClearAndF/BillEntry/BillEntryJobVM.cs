using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.ClearAndF.BillEntry
{
    public class BillEntryJobVM
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


        // new 

        public string BillNo { get; set; }
        public string BillDate { get; set; }
        public string CustomerAddress { get; set; }
        public string ExpDate { get; set; }
        public string LcNo { get; set; }
        public string LcDate { get; set; }
        public string LcValCurrencyId { get; set; }
        public decimal? BdtRate { get; set; }
        public decimal? BdtTotal { get; set; }
        public decimal? InvoiceValue { get; set; }
        public string InvValCurrencyId { get; set; }
        public string ConsoleNo { get; set; }
        public string DestinationId { get; set; }
        public decimal? AgComm { get; set; }
        public decimal? OnInvVal { get; set; }
        public decimal? Amount { get; set; }
        public string HeadId { get; set; }
    }

    public class ExpenseDetailDto
    {
        public string Head { get; set; } = string.Empty;
        public string IsRec { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public decimal? ConfirmAmt { get; set; }
        public decimal? BillAmt { get; set; }
    }

    public class ChargeTableItemDto
    {
        public string IsRec { get; set; } = string.Empty;
        public string Chc { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? BillAmt { get; set; }
        public decimal Tc { get; set; }
    }

    public class AddCostRequestDto
    {
        public int HeadId { get; set; }
        public decimal Cost { get; set; }
        public string? JobNo { get; set; }
        public string? CustomerId { get; set; }
        public decimal Tc { get; set; }
    }

    public class BillHistoryDto
    {
        public decimal Tc { get; set; }
        public string BillNo { get; set; } = string.Empty;
        public string BillDate { get; set; } = string.Empty;
        public string JobNo { get; set; } = string.Empty;
        public string ShipmentMode { get; set; } = string.Empty;
    }
}

