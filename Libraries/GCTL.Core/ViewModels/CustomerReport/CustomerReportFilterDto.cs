using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.CustomerReport
{
    public class CustomerReportFilterDto
    {
        public List<string> CustomerId { get; set; }   
        public List<string> CustomerTypeId { get; set; }
        public string CustomerAddress { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Fax { get; set; }
        public string ContactPersonName { get; set; }
        public string DesignationCode { get; set; }
        public string Designation { get; set; }
        public string ContactPersonMobile { get; set; }
        public string ContactPersonEmail { get; set; }
    }
}
