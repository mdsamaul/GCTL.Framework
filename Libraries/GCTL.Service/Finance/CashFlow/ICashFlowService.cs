using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.CashFlow;

namespace GCTL.Service.Finance.CashFlow
{
    public interface ICashFlowService
    {
        Task<CashFlowReportVM> GetCashFlowReportAsync(CashFlowFilterVM filter);
        Task<CashFlowReportVM> GetInitialViewModelAsync();
        Task<List<CommonSelectVM>> GetCompaniesAsync();
        Task<List<CommonSelectVM>> GetBranchesAsync(string? companyCode = null);
        Task<byte[]> GeneratePdfAsync(CashFlowFilterVM filter);
        Task<byte[]> ExportExcelAsync(CashFlowFilterVM filter);
    }
}
