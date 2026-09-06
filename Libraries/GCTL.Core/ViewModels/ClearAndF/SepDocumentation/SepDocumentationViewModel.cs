using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.ClearAndF.SepDocumentation
{
    public class SepDocumentationViewModel
    {
        public string ShipmentMode { get; set; }
        public bool IsCustomer { get; set; }

        [Required(ErrorMessage = "Job No is required")]
        [Display(Name = "Job No")]
        public string JobNo { get; set; }

        [Display(Name = "Customer ID")]
        public string CustomerID { get; set; }

        [Display(Name = "Customer Address")]
        public string CustomerAddress { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime? Date { get; set; }

        [Display(Name = "LC No")]
        public string LCNo { get; set; }

        [Display(Name = "LC Value")]
        [Range(0, double.MaxValue, ErrorMessage = "LC Value must be non-negative")]
        public decimal? LCValue { get; set; }

        [Display(Name = "IP No")]
        public string IPNo { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "IP Date")]
        public DateTime? IPDate { get; set; }

        [Display(Name = "Invoice No")]
        public string InvoiceNo { get; set; }

        [Display(Name = "Container No")]
        public string ContainerNo { get; set; }

        [Display(Name = "Material Description")]
        public string MaterialDescription { get; set; }

        [Display(Name = "Quantity 1")]
        [Range(0, double.MaxValue, ErrorMessage = "Quantity 1 must be non-negative")]
        public decimal? Quntity1 { get; set; }

        [Display(Name = "Quantity 2")]
        [Range(0, double.MaxValue, ErrorMessage = "Quantity 2 must be non-negative")]
        public decimal? Quntity2 { get; set; }

        [Display(Name = "FWDR Shipping Agent")]
        public string FWDR_ShippingAgent { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "ETA")]
        public DateTime? ETA { get; set; }

        [Display(Name = "BE No")]
        public string BENo { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "BE Date")]
        public DateTime? BEDate { get; set; }

        [Display(Name = "Shipment Status")]
        public string ShipmentStatus { get; set; }

        [Display(Name = "Remarks")]
        public string Remarks { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "L Date")]
        public DateTime? LDate { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Modify Date")]
        public DateTime? ModifyDate { get; set; }

        [Display(Name = "Customer ID Importer")]
        public string CustomerID_Importer { get; set; }
    }
}
