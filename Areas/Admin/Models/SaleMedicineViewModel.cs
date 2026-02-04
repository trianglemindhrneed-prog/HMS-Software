using Microsoft.AspNetCore.Mvc.Rendering;

namespace HMSCore.Areas.Admin.Models
{
    public class SaleMedicineViewModel
    {
        public string InvoiceId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public DateTime SaleDate { get; set; }

        public decimal GrandTotal { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal GstPercent { get; set; }
        public decimal FinalAmount { get; set; }

        public List<MedicineModel> Medicines { get; set; } = new List<MedicineModel>();
        public bool IsSaved { get; set; }
        public List<SelectListItem> PatientList { get; set; } = new List<SelectListItem>();


        public List<DropDownItem> Categories { get; set; } = new List<DropDownItem>();
    }

    public class MedicineModel
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string CategoryName { get; set; }
        public decimal MRP { get; set; }
        public int Quantity { get; set; }
        public int AvailableQty { get; set; }
        public decimal Total { get; set; }

        public int NoOfDays { get; set; }
        public int WhenToTake { get; set; }
        public int RequestedQty { get; set; }

    }

    public class DropDownItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
