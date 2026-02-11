using System.ComponentModel.DataAnnotations;

namespace HMSCore.Areas.Admin.Models
{
    public class LabTest
    {
        public int LabId { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int? LabCategoryId { get; set; }

        public string CategoryName { get; set; }

        [Required(ErrorMessage = "Lab Test Name is required")]
        public string LabTestName { get; set; }

        public string? Description { get; set; }

        public string Unit { get; set; }

        public string RefrenceManager { get; set; }

        public string TestPrice { get; set; }
    }

}
