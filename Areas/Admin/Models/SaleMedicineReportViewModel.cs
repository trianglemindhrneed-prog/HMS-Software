namespace HMSCore.Areas.Admin.Models
{
    public class SaleMedicineReportViewModel
    {
        public int Id { get; set; }
        public string InvoiceId { get; set; }
        public DateTime InvoiceDate { get; set; }

        public string PatientId { get; set; }
        public string PatientName { get; set; }

        public decimal GrandTotal { get; set; }
        public decimal FinalAmount { get; set; }
    }

    public class SaleMedicineReportPageVM
    {
        public string FilterColumn { get; set; }
        public string Keyword { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public List<SaleMedicineReportViewModel> Records { get; set; }
    }

}
