using System;
using System.ComponentModel.DataAnnotations;

namespace HMSCore.Areas.Admin.Models
{
    public class IpdAdmission
    {
        public int AdmissionId { get; set; }

        public string PatientID { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        public int? Age { get; set; }

        public string Gender { get; set; }

        public string Number { get; set; }

        [Required(ErrorMessage = "Doctor is required")]
        public int? DoctorId { get; set; }

        public int? BedCategoryId { get; set; }

        public int? BedId { get; set; }

        public DateTime? AdmissionDateTime { get; set; }

        public string InitialDiagnosis { get; set; }

        public decimal? AdvanceAmount { get; set; }

        public string Status { get; set; }
    }
}
