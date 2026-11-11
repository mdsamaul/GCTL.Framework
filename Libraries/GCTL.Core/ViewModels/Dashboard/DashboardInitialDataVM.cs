using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Dashboard
{
    public class DashboardSelectOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class DashboardInitialDataVM
    {
        public List<DashboardSelectOption> Branches { get; set; } = new();
        public List<DashboardSelectOption> ShipmentModes { get; set; } = new();
        public List<DashboardSelectOption> Ports { get; set; } = new();
        public List<DashboardSelectOption> Customers { get; set; } = new();
        public List<DashboardSelectOption> Statuses { get; set; } = new();
        public string DefaultFromDate { get; set; } = string.Empty;
        public string DefaultToDate { get; set; } = string.Empty;
    }
}
