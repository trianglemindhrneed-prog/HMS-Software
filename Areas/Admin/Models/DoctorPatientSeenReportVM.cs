namespace HMSCore.Areas.Admin.Models
{
   
    public class DoctorPatientSeenReportVM
    {
        public string DoctorName { get; set; }
        public string MobileNu { get; set; }
        public string Email { get; set; }
        public int PatientSeenCount { get; set; }
    } 
    public class DoctorPatientSeenPageVM
    {
        public int? TotalPatientsSeen { get; set; }
        public int? TotalDoctors { get; set; }
        public string? FilterColumn { get; set; }
        public string? Keyword { get; set; }
        public List<DoctorPatientSeenReportVM> Records { get; set; } = new List<DoctorPatientSeenReportVM>();
    }

}
