namespace HMSCore.Areas.Admin.Models
{
    public class AppointmentPrint
    {
        public string BookingId { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string PatientPhone { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string DoctorPhone { get; set; } = "";
        public string DoctorAddress { get; set; } = "";
        public DateTime AppointmentDate { get; set; }
        public string AppointmentTime { get; set; } = "";
    }
}
