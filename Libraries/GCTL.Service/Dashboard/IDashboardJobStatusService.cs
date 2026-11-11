using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;

namespace GCTL.Service.Dashboard
{
    public interface IDashboardJobStatusService
    {
        Task<DashboardJobStatusDTO> GetJobStatusDataAsync(DashboardFilterDTO filter);
    }
}
