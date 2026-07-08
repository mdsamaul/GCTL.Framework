using System.Threading.Tasks;
using GCTL.Service.Dashboard;
using GCTL.UI.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers;

public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index()
    {
        var userName = User.Identity?.Name ?? "User";
        var fullName = LoginInfo?.FullName ?? userName;
        var userId = LoginInfo?.Id ?? string.Empty;

        var summary = await _dashboardService.GetDashboardSummaryAsync(userId, fullName);

        var viewModel = new DashboardViewModel
        {
            GreetingMessage = summary.GreetingMessage,
            Stats = summary.Stats,
            RecentActivities = summary.RecentActivities,
            PerformanceMetrics = summary.PerformanceMetrics
        };

        return View(viewModel);
    }
}
