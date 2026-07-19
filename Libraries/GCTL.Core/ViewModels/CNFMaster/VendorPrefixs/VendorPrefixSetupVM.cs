using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GCTL.Core.ViewModels.CNFMaster.VendorPrefixs
{
    public class VendorPrefixSetupVM :BaseViewModel
    {
        public decimal Tc { get; set; }
        [Required]
        public string PrifixName { get; set; }
        public string? ShortName { get; set; }
        public string VendorPrefixID { get; set; }
    }
}
