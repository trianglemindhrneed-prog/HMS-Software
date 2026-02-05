namespace HMSCore.Areas.Admin.Models
{ 
        public class Medicine
        {
            public int Id { get; set; }
            public int? CId { get; set; }
            public string? CategoryName { get; set; } 
            public string ProductName { get; set; }
            public string? Description { get; set; }
            public int? Qty { get; set; }
            public double? MRP { get; set; }
            public double? CP { get; set; }
            public DateTime? Mfg { get; set; }
            public DateTime? ExpiryDate { get; set; }
        }
   

}
