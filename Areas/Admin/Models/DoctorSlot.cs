namespace HMSCore.Areas.Admin.Models
{
 
        public class DoctorSlot
        {
            public int DoctorId { get; set; }
            public int ScheduleId { get; set; }
            public string DepartmentName { get; set; }
            public string FullName { get; set; }
            public DateTime ScheduleDate { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string SlotDuration { get; set; }
            public string SessionType { get; set; }
            public string SlotTime { get; set; }
            public bool IsBlocked { get; set; }
        }
     

}
