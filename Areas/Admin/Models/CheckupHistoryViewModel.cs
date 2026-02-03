namespace HMSCore.Areas.Admin.Models
{
    public class CheckupHistoryViewModel
    {
        public string PatientId { get; set; }

        // Patient Profile
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string BloodGroup { get; set; }
        public DateTime? DOB { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
        public string ProfilePath { get; set; }

        public List<CheckupVM> Checkups { get; set; } = new();
    }




}
