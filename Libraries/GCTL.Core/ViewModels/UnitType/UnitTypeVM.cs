using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.UnitType
{
    public class UnitTypeVM
    {
        public int? TC { get; set; }
        public string? UnitTypID { get; set; }
        [Required(ErrorMessage = "Unit Type Name is required")]
        public string? UnitTypeName { get; set; }
        public string? ShortName { get; set; }
        public int? DecimalPlaces { get; set; }
        public DateTime? LDate { get; set; }
        public string LUser { get; set; }
        public string LIP { get; set; }

        public string LMAC { get; set; }
        public DateTime? ModifyDate { get; set; }
    }
}
