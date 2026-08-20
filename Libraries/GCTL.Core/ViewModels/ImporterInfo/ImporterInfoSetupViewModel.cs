using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GCTL.Core.ViewModels.ImporterInfo
{
    public class ImporterInfoSetupViewModel : BaseViewModel
    {
        public decimal? Tc { get; set; }
        [Required]
        public string? ImporterId { get; set; }
        [Required]
        public string? ImporterName { get; set; }

        public string? ImporterAddress { get; set; }

        public string? Phone { get; set; }

        public string? Website { get; set; }

        public string? Email { get; set; }

        public string? ImporterCode { get; set; }

        public string? CountryId { get; set; }

        public string? City { get; set; }

        public string? StateOrProvince { get; set; }

        public string? ZipCode { get; set; }

        public string? VatRegNo { get; set; }

        public string? Fax { get; set; }

        public string? Remarks { get; set; }

        public string? ContactPerson1 { get; set; }

        public string? Designation1 { get; set; }

        public string? Phone1 { get; set; }

        public string? Email1 { get; set; }

        public string? ContactPerson2 { get; set; }

        public string? Designation2 { get; set; }

        public string? Phone2 { get; set; }

        public string? Email2 { get; set; }

        public string? CompanyCode { get; set; }

        public string? EmployeeId { get; set; }
    }
}
