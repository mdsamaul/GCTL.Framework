using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.DataTables;

namespace GCTL.Service.TrialBalance
{
    public interface ITrialBalanceService
    {
        Task<TrialBalanceFilterOptionsDTO> GetFilterOptionsAsync();
        Task<PaginationResponse<TrialBalanceItemDTO>> GetGridDataAsync(TrialBalanceFilter filter);
        Task<(PaginationResponse<TrialBalanceItemDTO> GridData, TrialBalanceSummaryDTO Summary)> GetGridDataWithSummaryAsync(TrialBalanceFilter filter);
        Task<List<TrialBalanceItemDTO>> GetTrialBalanceDataAsync(TrialBalanceFilter filter);
        Task<TrialBalanceSummaryDTO> GetSummaryAsync(TrialBalanceFilter filter);
        Task<byte[]> GenerateExcelReportAsync(TrialBalanceFilter filter);
        Task<byte[]> GeneratePdfReportAsync(TrialBalanceFilter filter);
        Task<(byte[] FileBytes, string ContentType, string FileName)> ExportReportAsync(TrialBalanceFilter filter, string exportFormat);
    }
}
