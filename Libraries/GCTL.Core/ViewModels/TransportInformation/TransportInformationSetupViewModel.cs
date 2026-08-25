using GCTL.Core.ViewModels.Accounts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GCTL.Core.ViewModels.TransportInformation
{
    public class TransportInformationSetupViewModel : BaseViewModel
    {
        public int? Tc { get; set; }
        [Required]
        public string? VehicleId { get; set; }
        [Required]
        public string VehicleNo { get; set; }
        [Required]
        public string VehicleTypeId { get; set; }
        public string? VehicleType { get; set; }
        public string? Remarks { get; set; }
        public string? FactoryId { get; set; }
        public string? CompanyCode { get; set; }
        public string? PortNameId { get; set; }
        public string? PortName { get; set; }
        public string? EmployeeId { get; set; }
        public string? Phone { get; set; }
        public string? VendorId { get; set; }
    }
}
