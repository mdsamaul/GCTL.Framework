using System;

namespace GCTL.Core.ViewModels.Dashboard
{
    public class DashboardFilterDTO
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? BranchCode { get; set; } = string.Empty;
        public string? CompanyCode { get; set; } = string.Empty;
        public string? ShipmentMode { get; set; } = string.Empty;
        public string? Port { get; set; } = string.Empty;
        public string? CustomerID { get; set; } = string.Empty;
        public string? JobStatus { get; set; } = string.Empty;
    }
}
