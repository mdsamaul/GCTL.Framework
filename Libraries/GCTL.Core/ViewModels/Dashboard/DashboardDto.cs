using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Dashboard;

public class DashboardStatsDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployeesToday { get; set; }
    public double AttendanceRateToday { get; set; }
    public int PendingLeaveRequests { get; set; }
    public int ActiveProjects { get; set; }
}

public class DashboardActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty; // Bootstrap Icon class name, e.g. bi-person-check
    public string BadgeColor { get; set; } = "primary"; // e.g. success, danger, info
    public DateTime Timestamp { get; set; }
}

public class DashboardPerformanceDto
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}

public class DashboardSummaryDto
{
    public DashboardStatsDto Stats { get; set; } = new();
    public List<DashboardActivityDto> RecentActivities { get; set; } = new();
    public List<DashboardPerformanceDto> PerformanceMetrics { get; set; } = new();
    public string GreetingMessage { get; set; } = string.Empty;
}
