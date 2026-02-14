namespace HMSCore.Areas.Admin.Models
{
 
        public class IPDBillingLedgerModel
        {

        public int LedgerId { get; set; }
        public string PatientId { get; set; }
        public DateTime BillingDate { get; set; }
        public string ServiceType { get; set; }
        public string Description { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Discount { get; set; }
        public bool IsPaid { get; set; }
        public string CreatedBy { get; set; }
    }
    }

