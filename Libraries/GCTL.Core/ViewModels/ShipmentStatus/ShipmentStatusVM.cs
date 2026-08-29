using System;
using GCTL.Core.DataTables;

namespace GCTL.UI.Core.ViewModels.ShipmentMode
{
    public class ShipmentStatusVM
    {
        public int? AutoId { get; set; }

        [Searchable]
        public string? ShipmentStatusId { get; set; }

        [Searchable]
        public string? ShipmentStatus { get; set; }

        [Searchable]
        public string? ShortName { get; set; }

        public string? TransportModeTypeId { get; set; }

        [Searchable]
        public string? transportModeType { get; set; }

        public string? Luser { get; set; }
        public DateTime? Ldate { get; set; }
        public string? Lip { get; set; }
        public string? Lmac { get; set; }
        public DateTime? ModifyDate { get; set; }
    }
}