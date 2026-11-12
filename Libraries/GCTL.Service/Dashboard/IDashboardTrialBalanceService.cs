using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;

namespace GCTL.Service.Dashboard
{
    public interface IDashboardTrialBalanceService
    {
        Task<DashboardTrialBalanceDTO> GetTrialBalanceDataAsync(DashboardFilterDTO filter);
        Task<byte[]> GeneratePdfAsync(DashboardFilterDTO filter, int level);
        Task<byte[]> GenerateExcelAsync(DashboardFilterDTO filter, int level, bool hideZero);
    }
}
