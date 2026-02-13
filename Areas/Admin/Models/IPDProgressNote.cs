using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HMSCore.Areas.Admin.Models
{
    public class IPDProgressNote
    {
        [Key]
        public int NoteId { get; set; }

        [Required]
        public string PatientId { get; set; }

        public DateTime VisitDate { get; set; } = DateTime.Now;

        public string? Subjective { get; set; }
        public string? Objective { get; set; }
        public string? Assessment { get; set; }
        public string? Plans { get; set; }

        public string? EnteredBy { get; set; }

        public int? DoctorId { get; set; }

        public string? DoctorName { get; set; }
        public Doctor? Doctor { get; set; }
    }
}
