using System.Threading.Tasks;
using GCTL.Core.DataTables;

namespace GCTL.Service.GPAnalysisReport
{
    public interface IGPAnalysisReportService
    {
        Task<GPAnalysisReportFilterOptionsDTO> GetFilterOptionsAsync();
        Task<PaginationResponse<GPAnalysisReportDTO>> GetGridDataAsync(GPAnalysisReportFilter filter);
        Task<GPAnalysisReportSummaryDTO> GetSummaryAsync(GPAnalysisReportFilter filter);
        Task<byte[]> GeneratePdfReportAsync(GPAnalysisReportFilter filter);
        Task<byte[]> GenerateExcelReportAsync(GPAnalysisReportFilter filter);
        Task<(byte[] FileBytes, string ContentType, string FileName)> ExportReportAsync(GPAnalysisReportFilter filter, string exportFormat);
    }
}

