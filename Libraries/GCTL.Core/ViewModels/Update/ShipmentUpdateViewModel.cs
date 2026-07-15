using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.Update
{
    public class ShipmentUpdateViewModel : BaseViewModel
    {


        [Required(ErrorMessage = "ETA Delivery Date is required")]
        [DataType(DataType.Date)]
        public DateTime ETADeliveryDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ActualDeliveryDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? UnstuffingDate { get; set; }

        [Required(ErrorMessage = "Place of Loading is required")]
        public string PlaceOfLoadingID { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ETDDate { get; set; }
        public DateTime? Date { get; set; }
        public TimeOnly? Time { get; set; } 

        public string? ShedYardID { get; set; }

        public string? FreightChargeID { get; set; }

        [StringLength(500)]
        [Required(ErrorMessage = "Shipment Status is required")]
        public string ShipmentStatus { get; set; }

        [Required(ErrorMessage = "Job No is required")]
        public string? JobNo { get; set; }
        public decimal? AutoId { get; set; } = 0m;
       
        public string? UpdateRemarks { get; set; }
    }


}
