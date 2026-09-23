using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.DataTables;
using GCTL.Core.ViewModels.ClearAndF.BillEntry;

namespace GCTL.Service.BillService
{
    public interface IJobBillEntryReportService
    {
        Task<PaginationResponse<JobBillEntryReportItemDTO>> GetGridDataAsync(JobBillEntryReportFilterVM filter);
        Task<JobBillEntryReportSummaryDTO> GetSummaryAsync(JobBillEntryReportFilterVM filter);
        Task<byte[]> GeneratePdfAsync(JobBillEntryReportFilterVM filter);
        Task<List<string>> GetBillNumbersAsync();
    }
}
