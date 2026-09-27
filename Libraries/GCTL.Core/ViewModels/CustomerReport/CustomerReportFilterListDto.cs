using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.CustomerReport
{
    public class CustomerReportFilterListDto
    {
        public List<CustomerReportResultDto> CustomerName { get; set; }
        public List<CustomerReportResultDto> CustomerType { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public DateTime Date { get; set; }
    }
}
