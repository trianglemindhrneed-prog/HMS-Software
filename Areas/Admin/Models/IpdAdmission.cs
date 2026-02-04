using System;
using System.ComponentModel.DataAnnotations;

namespace HMSCore.Areas.Admin.Models
{
    public class IpdAdmission
    {
        
            public int AdmissionId { get; set; }

            [Required(ErrorMessage = "Patient ID is required")]
            public string PatientID { get; set; }

            [Required(ErrorMessage = "Patient name is required")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Age is required")]
            [Range(0, 120, ErrorMessage = "Age must be between 0 and 120")]
            public int? Age { get; set; }

            [Required(ErrorMessage = "Gender is required")]
            public string Gender { get; set; }

            [Required(ErrorMessage = "Contact number is required")]
            [RegularExpression(@"^\d{10}$", ErrorMessage = "Contact number must be exactly 10 digits")]
            public string Number { get; set; }

            [Required(ErrorMessage = "Doctor is required")]
            public int? DoctorId { get; set; }

            [Required(ErrorMessage = "Bed category is required")]
            public int? BedCategoryId { get; set; }

            [Required(ErrorMessage = "Bed is required")]
            public int? BedId { get; set; }

            [Required(ErrorMessage = "Admission date is required")]
            public DateTime? AdmissionDateTime { get; set; }

            [Required(ErrorMessage = "Initial diagnosis is required")]
            public string InitialDiagnosis { get; set; }

            [Required(ErrorMessage = "Advance amount is required")]
            [Range(0, 1000000, ErrorMessage = "Advance amount must be valid")]
            public decimal? AdvanceAmount { get; set; }

            public string Status { get; set; }
        }
    }

