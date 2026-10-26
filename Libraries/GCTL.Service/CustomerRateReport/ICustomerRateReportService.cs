using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.CustomerRateReport;

namespace GCTL.Service.CustomerRateReport
{
    public interface ICustomerRateReportService
    {
        Task<List<CommonSelectModel>> GetCustomerListAsync();
        Task<(List<CustomerRateReportItemDTO> Data, int TotalCount, int FilteredCount)> GetGridDataAsync(CustomerRateReportFilterVM filter);
        Task<CustomerRateReportSummaryDTO> GetSummaryAsync(string? customerIds);
        Task<byte[]> GeneratePdfAsync(CustomerRateReportFilterVM filter);
        Task<byte[]> GenerateExcelAsync(CustomerRateReportFilterVM filter);
    }
}
