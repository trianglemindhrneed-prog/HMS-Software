namespace HMSCore.Areas.Admin.Models
{
  
        public class IPDConsentForm
        {
            public int ConsentId { get; set; }
            public string PatientId { get; set; }
            public string ConsentType { get; set; }
            public DateTime? ConsentDate { get; set; }
            public string UploadedFilePath { get; set; }
            public string TakenBy { get; set; }
            public string Notes { get; set; }

            // Search
            public string FilterColumn { get; set; }
            public string FilterValue { get; set; }
            public string SearchValue { get; set; }
        }

    }

