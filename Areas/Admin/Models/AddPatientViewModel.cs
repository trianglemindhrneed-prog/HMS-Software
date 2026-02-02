using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HMSCore.Areas.Admin.Models
{
    public class AddPatientViewModel
    {
        public AddPatientViewModel()
        {
            Departments = new List<Department>();
            Doctors = new List<Doctor>();

            DepartmentList = new SelectList(new List<SelectListItem>());
            DoctorList = new SelectList(new List<SelectListItem>());
        }
        public bool IsSaved { get; set; } 

        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public DateTime? DOB { get; set; }
        public string Age { get; set; }
        public string Gender { get; set; }
        public string ContactNo { get; set; }
        public string Address { get; set; }

        public int? SelectedDepartmentId { get; set; }
        public int? SelectedDoctorId { get; set; }

        public List<Department> Departments { get; set; }
        public List<Doctor> Doctors { get; set; }

        public SelectList DepartmentList { get; set; }
        public SelectList DoctorList { get; set; }
         
        public string ConsultFee { get; set; } 
    }


}
