using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;

namespace GCTL.Service.Dashboard
{
    public interface IDashboardCashBankService
    {
        Task<DashboardCashBankSummaryDTO> GetCashBankSummaryAsync(DashboardFilterDTO filter);
    }
}

