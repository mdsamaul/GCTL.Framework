using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.VendorPrefix
{
    public class VendorPrefixSetupViewModel : BaseViewModel
    {
        public int? Tc { get; set; }
        [Required]
        public string VendorPrifixId { get; set; }
        [Required]
        public string PrifixName { get; set; }

        public string? ShortName { get; set; }
    }
}
