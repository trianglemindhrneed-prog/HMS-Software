using System.ComponentModel.DataAnnotations;

namespace HMSCore.Areas.Admin.Models
{
    public class TreatmentCharge
    {
       
        [Required]
        public string Treatment { get; set; }

        [Required]
        public decimal Charge { get; set; }
    }

    public class CreateBillViewModel
    {
      
        public string PatientId { get; set; }
        public string VID { get; set; }

        [Display(Name = "Bill No")]
        public string BillNo { get; set; }

        [Required]
        [Display(Name = "Patient No")]
        public string PatientNo { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Gender { get; set; }

        public string Age { get; set; }

        public DateTime BillDate { get; set; }

        public decimal ConsultFee { get; set; }

        public List<TreatmentCharge> Charges { get; set; } = new List<TreatmentCharge>();

        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidValue { get; set; }
        public decimal Balance { get; set; }
        public DateTime? ExpectedDate { get; set; }
        public decimal Advance { get; set; } 
    }
}
