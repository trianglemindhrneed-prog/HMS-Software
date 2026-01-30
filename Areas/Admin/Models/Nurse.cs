using System.ComponentModel.DataAnnotations.Schema;

namespace HMSCore.Areas.Admin.Models
{
    public class Nurse
    {
        public int NurseId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNu { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string? ProfileImagePath { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime EntryDate { get; set; } = DateTime.Now;
        [NotMapped]
        public IFormFile ProfileImage { get; set; }
    }

}
