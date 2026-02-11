using System.ComponentModel.DataAnnotations;

namespace HMSCore.Areas.Admin.Models
{
    public class LabCategory
    {
        public int LabCategoryId { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }
}
