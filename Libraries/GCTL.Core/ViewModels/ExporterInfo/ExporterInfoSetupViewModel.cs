using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GCTL.Core.ViewModels.ExporterInfo
{
    public class ExporterInfoSetupViewModel : BaseViewModel
    {
        public decimal? Tc { get; set; }
        [Required]
        public string ExporterId { get; set; }
        [Required]

        public string ExporterName { get; set; }

        public string? ExporterAddress { get; set; }

        public string? Phone { get; set; }

        public string? Website { get; set; }

        public string? Email { get; set; }

        public string? ExporterCode { get; set; }

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
