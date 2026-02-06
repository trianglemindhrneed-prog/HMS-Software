using System.ComponentModel.DataAnnotations;

namespace HMSCore.Areas.Admin.Models
{
    public class BedCategory
    {
        public int BedCategoryId { get; set; }
        public int BedPrice { get; set; }

        [Required]
        [Display(Name = "Bed Category Name")]
        public string BedCategoryName { get; set; }
        public string Description { get; set; }
    }
}
