using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;

namespace GCTL.Service.Dashboard
{
    public interface IDashboardOverviewService
    {
        Task<DashboardOverviewDTO> GetOverviewDataAsync(DashboardFilterDTO filter);
        Task<byte[]> GeneratePdfReportAsync(DashboardFilterDTO filter, string printUser = "");
        Task<byte[]> GenerateExcelReportAsync(DashboardFilterDTO filter, string printUser = "");
    }
}
