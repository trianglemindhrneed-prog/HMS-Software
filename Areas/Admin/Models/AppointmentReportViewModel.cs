namespace HMSCore.Areas.Admin.Models
{
    public class AppointmentReportViewModel
    {
        public int AppointmentId { get; set; }
        public int BookingId { get; set; }
        public string PatientName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string DoctorName { get; set; }
        public string DepartmentName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; }
    }
}
