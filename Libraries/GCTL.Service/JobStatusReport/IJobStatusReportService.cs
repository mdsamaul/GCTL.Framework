using GCTL.Core.ViewModels.JobStatusReport;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.JobStatusReport
{
    public interface IJobStatusReportService
    {
        Task<JobStatusSummaryDTO> GetSummaryAsync(string? customerId, string? shipmentModeId = null);
        Task<List<SeaJobReportItemDTO>> GetSeaJobsAsync(JobStatusReportFilterVM filter);
        Task<List<AirJobReportItemDTO>> GetAirJobsAsync(JobStatusReportFilterVM filter);
        Task<byte[]> GenerateExcelReportAsync(JobStatusReportFilterVM filter);
        Task<byte[]> GeneratePdfReportAsync(JobStatusReportFilterVM filter);
    }
}
