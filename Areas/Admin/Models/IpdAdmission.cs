using System;
using System.ComponentModel.DataAnnotations;

namespace HMSCore.Areas.Admin.Models
{
    public class IpdAdmission
    {
        public int AdmissionID { get; set; }
        public string?  PatientID { get; set; }

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
        public string? DoctorName { get; set; }
        public string? DoctorNumber { get; set; }

        public string Status { get; set; }


        // List of Patients
        public List<IpdAdmission> Patients { get; set; } = new List<IpdAdmission>();

        // Filter & Paging
        public int PageSize { get; set; } = 20;
        public string FilterColumn { get; set; }
        public string Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Yearly { get; set; }
        public int? HalfYearly { get; set; }
        public int? Monthly { get; set; }
    }
    }

