namespace HMSCore.Areas.Admin.Models
{
    public class Bed
    {
        public int BedId { get; set; }

        public int? BedCategoryId { get; set; }
        public string? CategoryName { get; set; } 
        public string BedNumber { get; set; }
        public string? Description { get; set; }
    }

}
