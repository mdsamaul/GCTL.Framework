using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Common;

namespace GCTL.Service.CustomerAgingReport
{
    public interface ICustomerAgingReportService
    {
        Task<CustomerAgingFilterOptionsDTO> GetFilterOptionsAsync();
        Task<List<CustomerAgingReportItemDTO>> GetReportDataAsync(CustomerAgingFilter filter);
        Task<CustomerAgingGridResult> GetGridDataAsync(CustomerAgingFilter filter);
        Task<byte[]> GeneratePdfReportAsync(CustomerAgingFilter filter, string printUser = "");
        Task<(byte[] FileBytes, string ContentType, string FileName)> ExportReportAsync(CustomerAgingFilter filter, string format, string printUser = "");
    }
}

