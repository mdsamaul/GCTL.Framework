namespace GCTL.Core.ViewModels.JobStatusReport
{
    public class JobStatusSummaryDTO
    {
        public int TotalJobs { get; set; }
        public int PendingJobs { get; set; }
        public int TransitJobs { get; set; }
        public int DeliveredJobs { get; set; }
        public int SeaCount { get; set; }
        public int AirCount { get; set; }
    }
}
