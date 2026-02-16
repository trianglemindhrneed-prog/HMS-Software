namespace HMSCore.Areas.Admin.Models
{
    public class IPDSaleMedicineReportViewModel
    {
        public int Id { get; set; }
        public string? InvoiceId { get; set; }
        public DateTime InvoiceDate { get; set; }

        public string? PatientId { get; set; }
        public string? PatientName { get; set; }

        public decimal GrandTotal { get; set; }
        public decimal FinalAmount { get; set; }
    }

    public class IPDSaleMedicineReportPageVM
    {
        public string? FilterColumn { get; set; }
        public string? Keyword { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        // Summary
        public decimal? YearlyPaid { get; set; }
        public decimal? HalfYearlyPaid { get; set; }
        public decimal? MonthlyPaid { get; set; }
        public List<IPDSaleMedicineReportViewModel> Records { get; set; }
    }


}
