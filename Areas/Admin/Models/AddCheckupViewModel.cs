using HMSCore.Areas.Admin.Models;
namespace HMSCore.Areas.Admin.Models
{
    public class AddCheckupViewModel
    {
        public string? PatientId { get; set; }

        public int? SelectedDoctorId { get; set; }
        public string? Symptoms { get; set; }
        public string? Diagnosis { get; set; }
        public DateTime? CheckupDate { get; set; }
        public string? ExtraNotes { get; set; }

        public List<Doctor> Doctors { get; set; } = new();
        public List<MedicineVM> Medicines { get; set; } = new();
        public List<PrescriptionVM> Prescriptions { get; set; } = new();
    }
}
