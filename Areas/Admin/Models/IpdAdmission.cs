using System;
using System.ComponentModel.DataAnnotations;

namespace HMSCore.Areas.Admin.Models
{
    public class IpdAdmission
    {
        public int AdmissionId { get; set; }

        [Display(Name = "Patient ID")]
        public string? PatientID { get; set; }

        [Required(ErrorMessage = "Patient name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 120, ErrorMessage = "Invalid age")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string? Gender { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [StringLength(15)]
        public string? Number { get; set; }

        [Required(ErrorMessage = "Doctor is required")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Bed category is required")]
        public int BedCategoryId { get; set; }

        [Required(ErrorMessage = "Bed is required")]
        public int BedId { get; set; }

        [Required]
        [Display(Name = "Admission Date & Time")]
        public DateTime AdmissionDateTime { get; set; } = DateTime.Now;

        [Display(Name = "Initial Diagnosis")]
        public string? InitialDiagnosis { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Invalid advance amount")]
        public decimal AdvanceAmount { get; set; }

        [Required]
        public string Status { get; set; } = "Admitted";
    }
}
