using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.BranchAddress
{
    public class BranchAddressSetupViewModel : BaseViewModel
    {
        public decimal? Tc { get; set; }   
        public string? VendorBranchId { get; set; }
        [Required]
        public string VendorId { get; set; }
        public string? Name { get; set; }
        [Required]
        public string LocationAddress { get; set; }
        public List<string>? ContactPerson { get; set; }
        public string? ContactPersonName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? DesignationCode { get; set; }
        public string? VendorName { get; set; }
        public string? CountryId { get; set; }
        public string? StateOrProvince { get; set; }
        public string? ZipCode { get; set; }
        public string? Remarks { get; set; }
        public string? City { get; set; }
    }
}
