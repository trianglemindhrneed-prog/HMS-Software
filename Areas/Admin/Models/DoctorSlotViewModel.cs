namespace HMSCore.Areas.Admin.Models
{
  
    public class DoctorSlotViewModel
    {
        public int PageSize { get; set; } = 20;
        public string FilterColumn { get; set; }
        public string Keyword { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int? SelectedDepartmentId
        {
            get => DepartmentId;
            set => DepartmentId = value;
        }

        public int? SelectedDoctorId
        {
            get => DoctorId;
            set => DoctorId = value;
        }

        public int? DepartmentId { get; set; }
        public int? DoctorId { get; set; }

        // CHANGE: string instead of bool
        public string ShowMorning { get; set; } = "true";
        public string ShowAfternoon { get; set; } = "true";
        public string ShowEvening { get; set; } = "true";

        // Helper properties to read as bool in code
        public bool IsMorning => ShowMorning == "true";
        public bool IsAfternoon => ShowAfternoon == "true";
        public bool IsEvening => ShowEvening == "true";

        public List<DoctorSlot> Slots { get; set; } = new List<DoctorSlot>();
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<Doctor> Doctors { get; set; } = new List<Doctor>();
        public void SetSessionFilters(string morning, string afternoon, string evening)
        {
            ShowMorning = morning;      // existing private string property
            ShowAfternoon = afternoon;
            ShowEvening = evening;
        }
    }
}
