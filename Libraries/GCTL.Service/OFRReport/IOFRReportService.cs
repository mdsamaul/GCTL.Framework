using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Common;

namespace GCTL.Service.OFRReport
{
    public interface IOFRReportService
    {
        Task<OFRReportFilterOptionsDTO> GetFilterOptionsAsync();
        Task<List<CommonSelectModel>> GetBranchesAsync(string? companyCode);
        Task<List<string>> GetJobNosAsync(string? customerId, string? shipmentModeId, string? fromDate, string? toDate);
        Task<List<OFRReportMasterDTO>> GetReportDataAsync(OFRReportFilter filter);
        Task<OFRReportGridResult> GetGridDataAsync(OFRReportFilter filter);
        Task<OFRReportSummaryDTO> GetSummaryAsync(OFRReportFilter filter);
        Task<(byte[] FileBytes, string ContentType, string FileName)> ExportReportAsync(OFRReportFilter filter, string exportFormat, string printUser = "");
        Task<byte[]> GeneratePdfReportAsync(OFRReportFilter filter, string printUser = "");
        Task<byte[]> GenerateExcelReportAsync(OFRReportFilter filter);
    }
}
