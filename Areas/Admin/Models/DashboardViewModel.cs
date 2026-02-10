using System;

namespace HMSCore.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        // OPD
        public int OPDTodaysPatients { get; set; }
        public int OPDNewPatients { get; set; }
        public int OPDTotalPatients { get; set; }
        public int TodaysAppointments { get; set; }
        public int TotalPatientsSeen { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalDueAmount { get; set; }
        public decimal TotalSellMedicine { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalNurses { get; set; }

        // IPD
        public int IPDNewPatients { get; set; }
        public int IPDTotalPatients { get; set; }
        public decimal TotalIPDSellMedicine { get; set; }
        public int AvailableBeds { get; set; }
        public int OccupiedBeds { get; set; }

        // Pathology
        public int LabCategoryCount { get; set; }
        public int LabTestCount { get; set; }
        public int PendingTests { get; set; }
        public int DeliveredTests { get; set; }

        // User
        public string UserRole { get; set; }
    }
}
