namespace GCTL.Core.ViewModels.Update
{
    // Models/JobStatusDto.cs
    public class JobStatusDto
    {
        public decimal Id { get; set; }
        public string Status { get; set; }           // e.g., Success, Warning, Danger
        public string JobNo { get; set; }
        public string ShipmentStatus { get; set; }
        //public string DateTime { get; set; }         // Formatted as "yyyy-MM-dd HH:mm"
        public string JobUpdateCode { get; set; }
        public string CustomerCode { get; set; }
        public DateTime? UpdateDate { get; set; }
        public DateTime? DateTime { get; set; }        // raw value from DB
        public string DateTimeText { get; set; } = ""; // formatted for display
    }
}