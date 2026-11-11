using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;

namespace GCTL.Service.Dashboard
{
    public interface IDashboardOverviewService
    {
        Task<DashboardOverviewDTO> GetOverviewDataAsync(DashboardFilterDTO filter);
    }
}
