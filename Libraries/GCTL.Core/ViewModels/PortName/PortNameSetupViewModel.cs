using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GCTL.Core.ViewModels.PortName
{
    public class PortNameSetupViewModel : BaseViewModel
    {
        public int? Tc { get; set; }
        [Required]
        public string PortNameId { get; set; }
        [Required]
        public string PortName { get; set; }

        public string? ShortName { get; set; }
        [Required]
        public string PortId { get; set; }
        public string? PortType { get; set; }


    }
}
