namespace HMSCore.Areas.Admin.Models
{
    public class BedAllotmentViewModel
    {
        public int BedAllotmentId { get; set; }
        public string BedCategory { get; set; }
        public string BedNumber { get; set; }
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public DateTime? AllotmentDate { get; set; }
        public DateTime? DischargeDate { get; set; }
    }

    public class BedAllotmentListViewModel
    {
        public List<BedAllotmentViewModel> BedAllotments { get; set; }
        public string FilterColumn { get; set; }
        public string Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
    }

}
