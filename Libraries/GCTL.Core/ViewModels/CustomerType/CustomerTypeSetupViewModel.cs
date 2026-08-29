using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.CustomerType
{
    public class CustomerTypeSetupViewModel : BaseViewModel
    {
        public decimal? AutoId { get; set; }

        public string CustomerTypeId { get; set; }

        public string CustomerType { get; set; }

        public string? ShortName { get; set; }

        public string? EmployeeId { get; set; }

        public string? CompanyId { get; set; }
    }
}
