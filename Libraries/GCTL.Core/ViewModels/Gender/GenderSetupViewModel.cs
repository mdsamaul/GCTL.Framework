using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.Gender
{
    public class GenderSetupViewModel: BaseViewModel
    {
        public decimal? AutoId { get; set; }
        public string? SexCode { get; set; }
        [Required(ErrorMessage = "Sex is required")]
        public string Sex { get; set; }
    }
}
