namespace HMSCore.Areas.Admin.Models
{

    public class PrescriptionVM
    {
        public int? MedicineId { get; set; }
        public string? MedicineName { get; set; }
        public string NoOfDays { get; set; }
        public string WhenToTake { get; set; }
        public bool IsBeforeMeal { get; set; }
    }
}
