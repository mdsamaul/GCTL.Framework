using GCTL.Core.ViewModels.TransportModeType;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GCTL.Core.ViewModels.TransportType
{
    public class TransportTypeSetupViewModel : BaseViewModel
    {
        public int? Tc { get; set; }
        [Required]
        public string VehicleTypeId { get; set; }
        [Required]
        public string VehicleType { get; set; }
        public string? ShortName { get; set; }
        public static implicit operator TransportTypeSetupViewModel(TransportModeTypeSetupViewModel v)
        {
            throw new NotImplementedException();
        }
    }
}
