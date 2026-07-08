using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;

namespace GCTL.Service.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(string userId, string fullName);
}
