using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace GCTL.Core.ViewModels.CfDefPlaceOfLoading
{
    public class CfDefPlaceOfLoadingSetupViewModel : BaseViewModel
    {
        public int? Tc { get; set; }

        [Required(ErrorMessage = "Place Of Loading ID is required.")]
        public string PlaceOfLoadingId { get; set; }

        [Required(ErrorMessage = "Place Of Loading is required.")]
        [Display(Name = "Place Of Loading")]
        public string PlaceOfLoading { get; set; }

        [Display(Name = "Short Name")]
        public string? ShortName { get; set; }
    }
}