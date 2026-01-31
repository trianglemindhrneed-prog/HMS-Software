using System;
using System.Collections.Generic;

namespace HMSCore.Models
{
    public class AppointmentViewModel
    {
        public int? SelectedDepartmentId { get; set; }
        public int? SelectedDoctorId { get; set; }
        public DateTime? SelectedDate { get; set; }
        public string SelectedSlot { get; set; }

        public string PatientName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public string PaymentMode { get; set; } = "Offline";

        public List<Department> Departments { get; set; } = new();
        public List<Doctor> Doctors { get; set; } = new();
        public List<string> AvailableSlots { get; set; } = new();
        public List<string> BookedSlots { get; set; } = new();
        public List<string> BlockedSlots { get; set; } = new();
        public List<string> TimeoutSlots { get; set; } = new();
    }

    public class Department { public int DepartmentId { get; set; } public string DepartmentName { get; set; } }
    public class Doctor { public int DoctorId { get; set; } public string FullName { get; set; } public string Address { get; set; } }
}

