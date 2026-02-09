namespace HMSCore.Areas.Admin.Models
{
    public class IPDVital
    {
        public int VitalsId { get; set; }
        public string PatientId { get; set; }

        public string Name { get; set; }
        public DateTime RecordedDate { get; set; }
        public string BloodGroup { get; set; }
        public string Temperature { get; set; }
        public string Pulse { get; set; }
        public string Height { get; set; }
        public string Weight { get; set; }
        public string BP { get; set; }
        public string SpO2 { get; set; }
      
        public string Notes { get; set; }
        public string RR { get; set; }
      
        public string RecordedBy { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

    }
  

}
