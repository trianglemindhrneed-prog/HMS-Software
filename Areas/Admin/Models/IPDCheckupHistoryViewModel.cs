namespace HMSCore.Areas.Admin.Models
{
    public class IPDCheckupHistoryViewModel
    {
        public string PatientId { get; set; }

        // Patient Profile
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string BloodGroup { get; set; }
        public string Age { get; set; }
        public string Number { get; set; }
        //public string Address { get; set; }
        public string ProfilePath { get; set; }

        public List<CheckupVM> Checkups { get; set; } = new();
    }
}
