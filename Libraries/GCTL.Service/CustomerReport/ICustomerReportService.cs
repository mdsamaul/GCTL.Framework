using GCTL.Core.ViewModels.CustomerReport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.CustomerReport
{
    public interface ICustomerReportService
    {
        Task<List<CustomerReportResultDto>> GetCustomerReportDataAsync(CustomerReportFilterDto filter);
        Task<byte[]> GenerateExcelReportAsync(CustomerReportFilterDto filter);
        Task<byte[]> GeneratePdfReportAsync(CustomerReportFilterDto filter);
    }
}
