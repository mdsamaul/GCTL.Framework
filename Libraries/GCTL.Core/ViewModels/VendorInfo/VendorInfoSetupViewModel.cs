using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.VendorInfo
{
    public class VendorInfoSetupViewModel : BaseViewModel
    {
        public decimal? Tc { get; set; }
        [Required]
        public string VendorId { get; set; }
        [Required]
        public string Name { get; set; }

        public string? Address { get; set; }

        public string? Phone { get; set; }

        public string? Website { get; set; }

        public string? Email { get; set; }

        public string? CompanyCode { get; set; }

        public string? EmployeeId { get; set; }
        [Required]
        public string VendorPrefix { get; set; }

        public string? VendorCode { get; set; }

        public string? CountryId { get; set; }

        public string? City { get; set; }

        public string? StateOrProvince { get; set; }

        public string? ZipCode { get; set; }

        public string? VatRegNo { get; set; }

        public string? Fax { get; set; }

        public List<string>? Cpid { get; set; }

        public string? Remarks { get; set; }
    }
}
