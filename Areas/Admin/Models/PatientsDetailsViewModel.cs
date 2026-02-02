namespace HMSCore.Areas.Admin.Models
{
    
    public class PatientsDetailsViewModel
    {
        // Individual Patient Fields
        public int Id { get; set; }
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string Age { get; set; }
        public string ContactNo { get; set; }
        public string Address1 { get; set; }
        public string ConsultFee { get; set; }
        public string DepartmentName { get; set; }
        public string DoctorName { get; set; }
        public string DoctorNumber { get; set; }
        public DateTime? CreatedDate { get; set; }

        // List of Patients
        public List<PatientsDetailsViewModel> Patients { get; set; } = new List<PatientsDetailsViewModel>();

        // Filter & Paging
        public int PageSize { get; set; } = 20;
        public string FilterColumn { get; set; }
        public string Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

}
