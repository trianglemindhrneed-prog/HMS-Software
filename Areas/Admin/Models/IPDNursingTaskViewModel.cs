namespace HMSCore.Areas.Admin.Models
{
    public class IPDNursingTaskViewModel
    {
        public int TaskId { get; set; }
        public string? PatientId { get; set; }
        public string? TaskName { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public DateTime? GivenDate { get; set; }
        public string? Status { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
    }
}
