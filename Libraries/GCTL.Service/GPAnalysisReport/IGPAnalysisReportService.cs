using System.Threading.Tasks;
using GCTL.Core.DataTables;

namespace GCTL.Service.GPAnalysisReport
{
    public interface IGPAnalysisReportService
    {
        Task<PaginationResponse<GPAnalysisReportDTO>> GetGridDataAsync(GPAnalysisReportFilter filter);
        Task<GPAnalysisReportSummaryDTO> GetSummaryAsync(GPAnalysisReportFilter filter);
        Task<byte[]> GeneratePdfReportAsync(GPAnalysisReportFilter filter);
        Task<byte[]> GenerateExcelReportAsync(GPAnalysisReportFilter filter);
    }
}
