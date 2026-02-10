using System;
using System.ComponentModel.DataAnnotations;

namespace HMSCore.Areas.Admin.Models
{
    public class IpdAdmission
    {
        public int AdmissionID { get; set; }
        public string PatientID { get; set; }

        public int? DoctorId { get; set; }
        public int? BedCategoryId { get; set; }
        public int? BedId { get; set; }

        public DateTime? AdmissionDateTime { get; set; }
        public string InitialDiagnosis { get; set; }

        public decimal? AdvanceAmount { get; set; }
        public string Name { get; set; }

        public int? Age { get; set; }
        public string Gender { get; set; }
        public string Number { get; set; }

        public string Status { get; set; }
    }
    }

