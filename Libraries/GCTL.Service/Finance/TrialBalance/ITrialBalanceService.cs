using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.TrialBalance;

namespace GCTL.Service.Finance.TrialBalance
{
    public interface ITrialBalanceService
    {
        Task<TrialBalanceReportVM> GetTrialBalanceReportAsync(TrialBalanceFilterVM filter);
        Task<TrialBalanceReportVM> GetInitialViewModelAsync();
        Task<List<CommonSelectVM>> GetCompaniesAsync();
        Task<List<CommonSelectVM>> GetBranchesAsync(string? companyCode = null);
        Task<byte[]> GeneratePdfAsync(TrialBalanceFilterVM filter);
    }
}
