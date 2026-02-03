namespace HMSCore.Areas.Admin.Models
{
    public class CheckupVM
    {
        public int CheckupId { get; set; }
        public DateTime CheckupDate { get; set; }
        public string DoctorName { get; set; }
        public string Symptoms { get; set; }
        public string Diagnosis { get; set; }
        public string ExtraNotes { get; set; }
        public List<PrescriptionVM> Prescriptions { get; set; } = new();
    }
}
