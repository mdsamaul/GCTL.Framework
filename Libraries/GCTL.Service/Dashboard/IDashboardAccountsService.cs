using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;

namespace GCTL.Service.Dashboard
{
    public interface IDashboardAccountsService
    {
        Task<DashboardAccountsDTO> GetAccountsDataAsync(DashboardFilterDTO filter);
        Task<DashboardBalanceSheetDTO> GetBalanceSheetDataAsync(DashboardFilterDTO filter);
    }
}
