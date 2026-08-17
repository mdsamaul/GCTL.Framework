using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GCTL.Core.ViewModels.PortType
{
    public class PortTypeSetupViewModel : BaseViewModel
    {
        public int? Tc { get; set; }

        public string PortId { get; set; }
        [Required(ErrorMessage = "Port Type is required")]
        public string PortType { get; set; }

        public string? ShortName { get; set; }
    }
}
