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
        Task<byte[]> GenerateExcelReportAsync(CustomerReportFilterDto filter);
        Task<byte[]> GeneratePdfReportAsync(CustomerReportFilterDto filter);
        Task<List<CustomerReportResultDto>> GetCustomerReportDataAsync(CustomerReportFilterDto filter, bool loadBinaryContent = true);
    }
}
