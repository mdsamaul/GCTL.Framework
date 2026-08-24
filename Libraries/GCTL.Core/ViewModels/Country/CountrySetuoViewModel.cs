using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.Country
{
    public class CountrySetuoViewModel : BaseViewModel
    {
        public int Tc { get; set; }
        public string? CountryId { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Country Name")]
        public string CountryName { get; set; }
        public string? Ioccode { get; set; }
        public string? Isocode { get; set; }
    }
}
