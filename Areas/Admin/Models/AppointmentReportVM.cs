namespace HMSCore.Areas.Admin.Models
{
    public class AppointmentReportVM
    {
        public List<AppointmentReportViewModel> Appointments { get; set; } = new();

        // Filters
        public string FilterColumn { get; set; }
        public string Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Paging
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalRecords { get; set; }
    }


}
