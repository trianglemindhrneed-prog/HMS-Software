using System.ComponentModel.DataAnnotations;

namespace HMSCore.Areas.Admin.Models
{
    
        public class PatientTestViewModel
        {
            public int ConsultantId { get; set; }
            public int PatientTestId { get; set; }
            public int? PatientTestDetailId { get; set; }
            public string? PatientId { get; set; }
            public string? PatientName { get; set; }
            public string? LabTestName { get; set; }
            public DateTime? TestDate { get; set; }
            public DateTime? DeliveryDate { get; set; }
            public int ReportStatus { get; set; }  
            public string? ReportPath { get; set; }
            public string? UserPatientsId { get; set; }

        public IFormFile? UploadReportFile { get; set; }
        public List<TestItemViewModel> Tests { get; set; } = new List<TestItemViewModel>();
    }
     
    public class TestItemViewModel
    {
        [Required(ErrorMessage = "Select Test")]
        public int? LabTestId { get; set; }


        public string Result { get; set; }
        public string Remarks { get; set; }
    }

    public class DropDownItems
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

}
