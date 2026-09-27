using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GCTL.Core.ViewModels.TransportModeType
{
    public class TransportModeTypeSetupViewModel : BaseViewModel
    {
        public int? Tc { get; set; }
        [Required]
        public string TransportModeId { get; set; }
        [Required]
        public string TransportMode { get; set; }

        public string? ShortName { get; set; }
    }
}
