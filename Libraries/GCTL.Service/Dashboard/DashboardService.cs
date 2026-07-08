using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;

namespace GCTL.Service.Dashboard;

public class DashboardService : IDashboardService
{
    public Task<DashboardSummaryDto> GetDashboardSummaryAsync(string userId, string fullName)
    {
        // Generate an appropriate greeting depending on the current time
        var currentHour = DateTime.Now.Hour;
        string greeting;
        if (currentHour < 12)
            greeting = "Good Morning";
        else if (currentHour < 17)
            greeting = "Good Afternoon";
        else
            greeting = "Good Evening";

        var summary = new DashboardSummaryDto
        {
            GreetingMessage = $"{greeting}, {fullName}!",
            Stats = new DashboardStatsDto
            {
                TotalEmployees = 148,
                ActiveEmployeesToday = 132,
                AttendanceRateToday = 89.2,
                PendingLeaveRequests = 7,
                ActiveProjects = 12
            },
            RecentActivities = new List<DashboardActivityDto>
            {
                new()
                {
                    Id = "ACT001",
                    Title = "Attendance Checked In",
                    Description = "Employee John Doe checked in at 09:05 AM.",
                    Icon = "bi-person-check",
                    BadgeColor = "success",
                    Timestamp = DateTime.Now.AddMinutes(-34)
                },
                new()
                {
                    Id = "ACT002",
                    Title = "Leave Request Submitted",
                    Description = "Sarah Jenkins submitted a Sick Leave request for 2 days.",
                    Icon = "bi-calendar-event",
                    BadgeColor = "warning",
                    Timestamp = DateTime.Now.AddHours(-2)
                },
                new()
                {
                    Id = "ACT003",
                    Title = "Project Created",
                    Description = "New project 'HRM Portal V2' was initialized by Admin.",
                    Icon = "bi-folder-plus",
                    BadgeColor = "info",
                    Timestamp = DateTime.Now.AddHours(-4)
                },
                new()
                {
                    Id = "ACT004",
                    Title = "Payroll Processed",
                    Description = "June payroll batches completed and ready for distribution.",
                    Icon = "bi-credit-card",
                    BadgeColor = "primary",
                    Timestamp = DateTime.Now.AddDays(-1)
                },
                new()
                {
                    Id = "ACT005",
                    Title = "System Health Alert",
                    Description = "Scheduled database backup completed successfully.",
                    Icon = "bi-shield-check",
                    BadgeColor = "secondary",
                    Timestamp = DateTime.Now.AddDays(-1).AddHours(-6)
                }
            },
            PerformanceMetrics = new List<DashboardPerformanceDto>
            {
                new() { Label = "Mon", Value = 94.0 },
                new() { Label = "Tue", Value = 95.5 },
                new() { Label = "Wed", Value = 91.2 },
                new() { Label = "Thu", Value = 93.8 },
                new() { Label = "Fri", Value = 89.2 }
            }
        };

        return Task.FromResult(summary);
    }
}
