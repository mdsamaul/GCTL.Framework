namespace GCTL.Core.ViewModels.JobStatusReport
{
    public class JobStatusReportFilterVM
    {
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? CustomerId { get; set; }
        public string? ShipmentMode { get; set; }
        public string? PortType { get; set; }
        public string? JobStatus { get; set; }
        public string? ExportFormat { get; set; } = "PDF";
        public string? ActiveTab { get; set; } = "Sea";
        public string? ReportOption { get; set; } = "Job Report";
    }
}
