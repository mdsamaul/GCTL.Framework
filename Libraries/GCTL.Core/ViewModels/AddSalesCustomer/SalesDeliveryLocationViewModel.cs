using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCTL.Core.DataTables;

namespace GCTL.Core.ViewModels.AddSalesCustomer
{
    public class SalesDeliveryLocationViewModel
    {
        [Searchable]
        public string? DeliveryLocationCode { get; set; } = null!;
        public string? CustomerId { get; set; }
        [Searchable]
        public string? CustomerName { get; set; }
        public string? DesignationCode { get; set; }
        [Searchable]
        public string? LocationAddress { get; set; }
        public string? CountryId { get; set; }
        public string? City { get; set; }
        public string? StateOrProvince { get; set; }
        public string? ZipCode { get; set; }
        [Searchable]
        public string? Phone { get; set; }
        [Searchable]
        public string? Email { get; set; }
        public string? ContactPerson { get; set; }
        public string? Remarks { get; set; }
        [Searchable]
        public string? ContactPersonName { get; set; }
        //New Added
        public string? DeliveryCode { get; set; }
    }
}
