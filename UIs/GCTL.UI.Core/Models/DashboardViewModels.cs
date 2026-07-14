using System.Collections.Generic;
using GCTL.Core.ViewModels.Dashboard;

namespace GCTL.UI.Core.Models;

public class DashboardViewModel
{
    public string GreetingMessage { get; set; } = string.Empty;
    public DashboardStatsDto Stats { get; set; } = new();
    public List<DashboardActivityDto> RecentActivities { get; set; } = new();
    public List<DashboardPerformanceDto> PerformanceMetrics { get; set; } = new();
}
