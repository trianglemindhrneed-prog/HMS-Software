namespace HMSCore.Areas.Admin.Models
{
    public class PatientBillViewModel
    {
        public int BillId { get; set; }
        public string BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public string PatientNo { get; set; }
        public string PatientID { get; set; }
        public string Name { get; set; }
        public string Age { get; set; }
        public string Gender { get; set; }
        public decimal? ConsultFee { get; set; }
        public decimal? GrandTotal { get; set; }

        // For paging & filtering
        public string FilterColumn { get; set; }
        public string Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageSize { get; set; } = 20;

        // This should be a list of bills, not a string
        public List<PatientBillViewModel> Bills { get; set; } = new List<PatientBillViewModel>();
    }
}
