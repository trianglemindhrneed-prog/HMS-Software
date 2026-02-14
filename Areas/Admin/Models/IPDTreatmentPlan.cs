namespace HMSCore.Areas.Admin.Models
{
    public class IPDTreatmentPlan
    {
        public int PlanId { get; set; }
        public string PatientId { get; set; }
        public int? TreatmentDay { get; set; }
        public DateTime TreatmentDate { get; set; }
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }
        public string DietPlan { get; set; }
        public string Notes { get; set; }
        public string EnteredBy { get; set; }
    }
}
