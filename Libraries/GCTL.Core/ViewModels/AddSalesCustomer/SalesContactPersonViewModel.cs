using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.AddSalesCustomer
{
    public class SalesContactPersonViewModel 
    {
        public decimal? AutoId { get; set; }

        public string Cpid { get; set; } = null!;

        public string ContactPersonName { get; set; } = null!;

        public string? DesignationCode { get; set; }

        public string ContactPersonMobile { get; set; } = null!;

        public string ContactPersonEmail { get; set; } = null!;

        public string? Luser { get; set; }

        public DateTime? Ldate { get; set; }

        public string? Lip { get; set; }

        public string? Lmac { get; set; }

        public DateTime? ModifyDate { get; set; }

        public string? CompanyCode { get; set; }

        public string? EmployeeId { get; set; }
        public string? DesignationName { get; set; }
    }
}
