using System;
using System.Collections.Generic;

namespace HMSCore.Areas.Admin.Models
{
    public class IPDCheckupHistoryPageVM
    {
        public IPDPatientProfileVM Patient { get; set; } = new();
        public List<IPDCheckupVM> Checkups { get; set; } = new();
    }

    public class IPDPatientProfileVM
    {
        public string PatientID { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public string Number { get; set; }
        public string Address { get; set; }
        public string ProfilePath { get; set; }
    }

    public class IPDCheckupVM
    {
        public int CheckupId { get; set; }
        public DateTime? CheckupDate { get; set; }
        public string DoctorName { get; set; }
        public string Symptoms { get; set; }
        public string Diagnosis { get; set; }
        public string ExtraNotes { get; set; }
        public List<IPDPrescriptionVM> Prescriptions { get; set; } = new();
    }

    public class IPDPrescriptionVM
    {
        public string MedicineName { get; set; }
        public string NoOfDays { get; set; }
        public string WhenToTake { get; set; }
        public bool IsBeforeMeal { get; set; }
    }
}
