using System.ComponentModel.DataAnnotations;

namespace HMSCore.Areas.Admin.Models
{
     public class Doctor
        {
            public int DoctorId { get; set; }
            public int DepartmentId { get; set; }

            public string FullName { get; set; }
            public string DepartmentName { get; set; }
            public string DEmail { get; set; }
            public string MobileNu { get; set; }
            public string Address { get; set; }

            public bool IsActive { get; set; }
        [StringLength(20, MinimumLength = 6,
          ErrorMessage = "Password must be between 6 and 20 characters.")]
        public string Password { get; set; } 
            public string ProfileImagePath { get; set; }

            public List<Department> Departments { get; set; }
        }
    



}
