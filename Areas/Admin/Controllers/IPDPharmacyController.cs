
using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HMSCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class IPDPharmacyController : BaseController
    {
        private readonly IDbLayer _dbLayer;
        private readonly IConfiguration _configuration;

        public IPDPharmacyController(IDbLayer dbLayer, IConfiguration configuration)
        {
            _dbLayer = dbLayer;
            _configuration = configuration;
        }
        // GET: Sale Medicine Page
        [HttpGet]
        public async Task<IActionResult> SaleMedicine()
        {
            var model = new IPDSaleMedicineViewModel();

            // 🔥 Generate next invoice number
            var dtInvoice = await _dbLayer.ExecuteSPAsync(
                "sp_IpdMedicineSale",
                new[] { new SqlParameter("@Action", "GetNextInvoice") }
            );

            model.InvoiceId = dtInvoice.Rows[0]["InvoiceId"].ToString();
            model.SaleDate = DateTime.Now;

            // Load dropdowns
            await LoadDropdowns(model);

            return View(model);
        }

        // Helper: Load dropdowns
        private async Task LoadDropdowns(IPDSaleMedicineViewModel model)
        {
            // Patients
            var dtPatients = await _dbLayer.ExecuteSPAsync(
                "sp_IpdMedicineSale",
                new[] { new SqlParameter("@Action", "GetPatients") }
            );

            model.PatientList = new List<SelectListItem>();
            foreach (DataRow row in dtPatients.Rows)
            {
                model.PatientList.Add(new SelectListItem
                {
                    Value = row["PatientId"].ToString(),
                    Text = row["Name"].ToString()
                });
            }


            // Medicine Categories
            // Medicine Categories
            var dtCategories = await _dbLayer.ExecuteSPAsync(
                "sp_IpdMedicineSale",
                new[] { new SqlParameter("@Action", "GetMedicineCategories") }
            );

            model.Categories = new List<IPDDropDownItem>();
            foreach (DataRow row in dtCategories.Rows)
            {
                model.Categories.Add(new IPDDropDownItem
                {
                    Id = row["CategoryId"].ToString(),
                    Name = row["CategoryName"].ToString()
                });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetPatientName(string patientId)
        {
            var dt = await _dbLayer.ExecuteSPAsync("sp_IpdMedicineSale",
                new[] { new SqlParameter("@Action", "GetPatientName"),
                new SqlParameter("@PatientId", patientId)});
            if (dt.Rows.Count > 0)
                return Json(new { name = dt.Rows[0]["Name"].ToString() });
            return Json(new { name = "" });
        }
        [HttpGet]
        public async Task<IActionResult> GetPatientCheckupDates(string patientId)
        {
            var dt = await _dbLayer.ExecuteSPAsync(
                "sp_IpdMedicineSale",
                new[] {
            new SqlParameter("@Action", "GetPatientCheckupDates"),
            new SqlParameter("@PatientId", patientId)
                });

            var list = dt.AsEnumerable().Select(row => new {
                CheckupId = Convert.ToInt32(row["CheckupId"]),
                CheckupDateStr = row["CheckupDateStr"]?.ToString() ?? ""
            }).ToList();


            return Json(list);
        }


        [HttpGet]
        public async Task<IActionResult> GetMedicinesByCheckup(int checkupId)
        {
            try
            {
                var medicines = new List<IPDMedicineModel>();

                var dt = await _dbLayer.ExecuteSPAsync(
                    "sp_IpdMedicineSale",
                    new[] {
                new SqlParameter("@Action","GetMedicinesByCheckup"),
                new SqlParameter("@CheckupId", checkupId)
                    }
                );

                if (dt == null || dt.Rows.Count == 0)
                    return Json(medicines);

                foreach (DataRow row in dt.Rows)
                {
                    int SafeInt(object val) => int.TryParse(val?.ToString(), out int x) ? x : 0;
                    decimal SafeDecimal(object val) => decimal.TryParse(val?.ToString(), out decimal x) ? x : 0;
                    string SafeString(object val) => val?.ToString() ?? "";

                    // Parse SP values safely
                    int noOfDays = SafeInt(row["NoOfDays"]);
                    int whenToTake = SafeInt(row["WhenToTake"]);
                    int requestedQty = SafeInt(row["RequestedQty"]);
                    int availableQty = SafeInt(row["AvailableQty"]);

                    int quantity = SafeInt(row["Quantity"]);
                    if (quantity == 0)
                        quantity = requestedQty > availableQty ? availableQty : requestedQty;

                    decimal mrp = SafeDecimal(row["MRP"]);

                    medicines.Add(new IPDMedicineModel
                    {
                        MedicineId = SafeInt(row["MedicineId"]),
                        MedicineName = SafeString(row["MedicineName"]),
                        CategoryName = SafeString(row["CategoryName"]),
                        MRP = mrp,
                        AvailableQty = availableQty,
                        NoOfDays = noOfDays,
                        WhenToTake = whenToTake,
                        RequestedQty = requestedQty,
                        Quantity = quantity,
                        Total = mrp * quantity
                    });
                }

                return Json(medicines);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"Server Error: {msg}");
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetMedicinesByCategory(int categoryId)
        {
            var medicines = new List<object>();

            // Execute stored procedure to get medicines by category
            var dt = await _dbLayer.ExecuteSPAsync(
                "sp_IpdMedicineSale",
                new[]
                {
            new SqlParameter("@Action", "GetMedicinesByCategory"),
            new SqlParameter("@CategoryId", categoryId)
                }
            );

            foreach (DataRow row in dt.Rows)
            {
                medicines.Add(new
                {
                    id = Convert.ToInt32(row["Id"]),
                    name = row["name"]?.ToString() ?? string.Empty
                });
            }

            return Json(medicines);
        }


        [HttpGet]
        public async Task<IActionResult> GetMedicineDetails(int productId)
        {
            // Execute stored procedure to get medicine details
            var dt = await _dbLayer.ExecuteSPAsync(
                "sp_IpdMedicineSale",
                new[]
                {
            new SqlParameter("@Action", "GetMedicineDetails"),
            new SqlParameter("@MedicineId", productId)
                }
            );

            if (dt.Rows.Count == 0)
            {
                return Json(new { qty = 0, mrp = 0 });
            }

            var row = dt.Rows[0];

            return Json(new
            {
                qty = Convert.ToInt32(row["qty"]),
                mrp = Convert.ToDecimal(row["MRP"])
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaleMedicine(IPDSaleMedicineViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model);
                return View(model);
            }

            try
            {
                var parameters = new[]
                {
            new SqlParameter("@Action", "SaveSale"),
            new SqlParameter("@InvoiceId", model.InvoiceId),
            new SqlParameter("@PatientId", model.PatientId),
            new SqlParameter("@PatientName", model.PatientName ?? (object)DBNull.Value),
            new SqlParameter("@InvoiceDate", model.SaleDate),
            new SqlParameter("@GrandTotal", model.GrandTotal),
            new SqlParameter("@DiscountPercent", model.DiscountPercent),
            new SqlParameter("@GstPercent", model.GstPercent),
            new SqlParameter("@FinalAmount", model.FinalAmount),
            new SqlParameter("@MedicinesJson", model.MedicinesJson)
        };

                await _dbLayer.ExecuteSPAsync("sp_IpdMedicineSale", parameters);

                model.IsSaved = true;   // ⭐ VERY IMPORTANT

                TempData["Message"] = $"Sale {model.InvoiceId} saved successfully!";
                TempData["MessageType"] = "success";

                model.Medicines = new List<IPDMedicineModel>();
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error while saving sale: " + ex.Message;
                TempData["MessageType"] = "error";
            }

            await LoadDropdowns(model);
            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> PrintMedicineBill(string pageName, string id)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler);

            var baseUrl = _configuration["WebFormBaseUrl"];
            var url = $"{baseUrl}/{pageName}.aspx?ID={id}";

            var pdfBytes = await client.GetByteArrayAsync(url);

            Response.Headers.Add("Content-Disposition", "inline; filename=Patient_{id}.pdf");
            return File(pdfBytes, "application/pdf");
        }

        //=========================BillSaleMedicine===============


        [HttpGet]
        public async Task<IActionResult> BillSaleMedicine(
     string filterColumn = null,
     string keyword = null,
     DateTime? fromDate = null,
     DateTime? toDate = null)
        {
            var dt = await _dbLayer.ExecuteSPAsync(
                "SP_IPDSaleMedicineReport", 
                new[]
                {
            new SqlParameter("@FilterColumn", (object)filterColumn ?? DBNull.Value),
            new SqlParameter("@Keyword", (object)keyword ?? DBNull.Value),
            new SqlParameter("@FromDate", (object)fromDate ?? DBNull.Value),
            new SqlParameter("@ToDate", (object)toDate?.AddDays(1) ?? DBNull.Value)
                });

            var list = dt.AsEnumerable().Select(r => new IPDSaleMedicineReportViewModel
            {
                Id = Convert.ToInt32(r["Id"]),
                InvoiceId = r["InvoiceId"].ToString(),
                InvoiceDate = Convert.ToDateTime(r["InvoiceDate"]),
                PatientId = r["PatientId"].ToString(),
                PatientName = r["PatientName"].ToString(),
                GrandTotal = Convert.ToDecimal(r["GrandTotal"]),
                FinalAmount = Convert.ToDecimal(r["FinalAmount"])
            }).ToList();

            return View(new IPDSaleMedicineReportPageVM
            {
                FilterColumn = filterColumn,
                Keyword = keyword,
                FromDate = fromDate,
                ToDate = toDate,
                Records = list
            });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMedicineBill(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Message"] = "Invalid invoice";
                TempData["MessageType"] = "error";
                return RedirectToAction("BillSaleMedicine");
            }

            await _dbLayer.ExecuteSPAsync(
                "SP_IPDSaleMedicineReport",
                new[]
                {
            new SqlParameter("@Action", "DELETE"),
            new SqlParameter("@InvoiceId", id)
                });

            TempData["Message"] = "Invoice deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("BillSaleMedicine");
        }


        [HttpPost]
        public async Task<IActionResult> DeleteSelectedMedicine(string[] selectedIds)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData["Message"] = "No invoice selected";
                TempData["MessageType"] = "error";
                return RedirectToAction("BillSaleMedicine");
            }

            foreach (var invoiceId in selectedIds)
            {
                await _dbLayer.ExecuteSPAsync(
                    "SP_IPDSaleMedicineReport",
                    new[]
                    {
                new SqlParameter("@Action", "DELETE"),
                new SqlParameter("@InvoiceId", invoiceId)
                    });
            }

            TempData["Message"] = "Selected invoices deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("BillSaleMedicine");
        }



        //[HttpGet]
        //public async Task<IActionResult> PrintMedicineBillOther(string pageName, string id)
        //{
        //    var handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        //    };

        //    using var client = new HttpClient(handler);

        //    var baseUrl = _configuration["WebFormBaseUrl"];
        //    var url = $"{baseUrl}/{pageName}.aspx?ID={id}";

        //    var pdfBytes = await client.GetByteArrayAsync(url);

        //    Response.Headers.Add("Content-Disposition", "inline; filename=Patient_{id}.pdf");
        //    return File(pdfBytes, "application/pdf");
        //}




    }
}
