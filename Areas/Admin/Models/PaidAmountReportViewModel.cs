using System;
using System.Collections.Generic;

namespace HMSCore.Areas.Admin.Models
{
    public class PaidAmountReportViewModel
    {
        public int PageSize { get; set; } = 20;
        public string FilterColumn { get; set; }
        public string Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Summary
        public decimal YearlyPaid { get; set; }
        public decimal HalfYearlyPaid { get; set; }
        public decimal MonthlyPaid { get; set; }
        public decimal YearlyDue { get; set; }
        public decimal HalfYearlyDue { get; set; }
        public decimal MonthlyDue { get; set; }

        // Patients List
        public List<PaidPatientDetails> Patients { get; set; } = new List<PaidPatientDetails>();
    }

    public class PaidPatientDetails
    {
        public int BillId { get; set; }
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string Age { get; set; }
        public string ContactNo { get; set; }
        public string Address1 { get; set; }
        public string BillNo { get; set; }
        public string DepartmentName { get; set; }
        public string DoctorName { get; set; }
        public string DoctorNumber { get; set; }
        public string ConsultFee { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public DateTime? BillDate { get; set; }
        public string Status { get; set; }
    }
}
