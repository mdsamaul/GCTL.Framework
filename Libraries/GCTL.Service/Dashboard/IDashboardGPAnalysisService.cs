using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;

namespace GCTL.Service.Dashboard
{
    public interface IDashboardGPAnalysisService
    {
        Task<DashboardGPAnalysisDTO> GetGPAnalysisDataAsync(DashboardFilterDTO filter);
    }
}
