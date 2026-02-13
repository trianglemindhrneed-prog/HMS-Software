using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;

namespace HMSCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PharmacyController : BaseController
    {
        private readonly IDbLayer _dbLayer;
        private readonly IConfiguration _configuration;

        public PharmacyController(IDbLayer dbLayer, IConfiguration configuration)
        {
            _dbLayer = dbLayer;
            _configuration = configuration;
        }

        // GET: Sale Medicine Page
        [HttpGet]
        public async Task<IActionResult> SaleMedicine()
        {
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Account", new { area = "" });
            var model = new SaleMedicineViewModel();

            // 🔥 Generate next invoice number
            var dtInvoice = await _dbLayer.ExecuteSPAsync(
                "sp_MedicineSale",
                new[] { new SqlParameter("@Action", "GetNextInvoice") }
            );

            model.InvoiceId = dtInvoice.Rows[0]["InvoiceId"].ToString();
            model.SaleDate = DateTime.Now;

            // Load dropdowns
            await LoadDropdowns(model);

            return View(model);
        }
         
        // Helper: Load dropdowns
        private async Task LoadDropdowns(SaleMedicineViewModel model)
        {
            // Patients
            var dtPatients = await _dbLayer.ExecuteSPAsync(
                "sp_MedicineSale",
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
                "sp_MedicineSale",
                new[] { new SqlParameter("@Action", "GetMedicineCategories") }
            );

            model.Categories = new List<DropDownItem>();
            foreach (DataRow row in dtCategories.Rows)
            {
                model.Categories.Add(new DropDownItem
                {
                    Id = row["CategoryId"].ToString(),     
                    Name = row["CategoryName"].ToString()   
                });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetPatientName(string patientId)
        {
            var dt = await _dbLayer.ExecuteSPAsync("sp_MedicineSale",
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
                "sp_MedicineSale",
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
                var medicines = new List<MedicineModel>();

                var dt = await _dbLayer.ExecuteSPAsync(
                    "sp_MedicineSale",
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

                    medicines.Add(new MedicineModel
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
                "sp_MedicineSale",
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
                "sp_MedicineSale",
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
        public async Task<IActionResult> SaleMedicine(SaleMedicineViewModel model)
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

                await _dbLayer.ExecuteSPAsync("sp_MedicineSale", parameters);

                model.IsSaved = true;   // ⭐ VERY IMPORTANT

                TempData["Message"] = $"Sale {model.InvoiceId} saved successfully!";
                TempData["MessageType"] = "success";

                model.Medicines = new List<MedicineModel>();
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


        [HttpGet]
        public async Task<IActionResult> BillSaleMedicine(
     string filterColumn = null,
     string keyword = null,
     DateTime? fromDate = null,
     DateTime? toDate = null)
        {
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Account", new { area = "" });
            var dt = await _dbLayer.ExecuteSPAsync(
                "SP_SaleMedicineReport",
                new[]
                {
            new SqlParameter("@FilterColumn", (object)filterColumn ?? DBNull.Value),
            new SqlParameter("@Keyword", (object)keyword ?? DBNull.Value),
            new SqlParameter("@FromDate", (object)fromDate ?? DBNull.Value),
            new SqlParameter("@ToDate", (object)toDate?.AddDays(1) ?? DBNull.Value)
                });

            var list = dt.AsEnumerable().Select(r => new SaleMedicineReportViewModel
            {
                Id = Convert.ToInt32(r["Id"]),
                InvoiceId = r["InvoiceId"].ToString(),
                InvoiceDate = Convert.ToDateTime(r["InvoiceDate"]),
                PatientId = r["PatientId"].ToString(),
                PatientName = r["PatientName"].ToString(),
                GrandTotal = Convert.ToDecimal(r["GrandTotal"]),
                FinalAmount = Convert.ToDecimal(r["FinalAmount"])
            }).ToList();

            return View(new SaleMedicineReportPageVM
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
                "SP_SaleMedicineReport",
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
                    "SP_SaleMedicineReport",
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
         
       
        [HttpGet]
        public async Task<IActionResult> CategoryList(string search = null, int? status = null)
        {
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Account", new { area = "" });
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "Select"),
                new SqlParameter("@FilterVal", string.IsNullOrEmpty(search) ? DBNull.Value : search),
                new SqlParameter("@Status", status.HasValue ? status.Value : (object)DBNull.Value)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManagePharamcycategory", parameters);

            var departments = dt.AsEnumerable().Select(r => new Category
            {
                CategoryId = Convert.ToInt32(r["CategoryId"]),
                CategoryName = r["CategoryName"].ToString(),
                Description = r["Description"].ToString(),
               
            }).ToList();

            ViewData["Search"] = search;
            ViewData["Status"] = status;

            return View(departments);
        }


        [HttpGet]
        public async Task<IActionResult> AddCategory(int? id)
        {
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Account", new { area = "" });
            if (id == null)
            {
                // Adding a new category
                return View(new Category());
            }

            // Editing existing category
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Action", "SelectBYId"),
        new SqlParameter("@CategoryId", id)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManagePharamcycategory", parameters);

            if (dt.Rows.Count == 0) return NotFound();

            var category = new Category
            {
                CategoryId = Convert.ToInt32(dt.Rows[0]["CategoryId"]),
                CategoryName = dt.Rows[0]["CategoryName"].ToString(),
                Description = dt.Rows[0]["Description"].ToString()
            };

            return View(category);
        }


        [HttpPost]
        public async Task<IActionResult> AddCategory(Category model)
        {
            ModelState.Remove("Description");
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Please fill all required fields.";
                TempData["MessageType"] = "error";
                return View(model);
            }

            try
            {
                string action = model.CategoryId > 0 ? "Update" : "Insert";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@Action", action),
            new SqlParameter("@CategoryId", model.CategoryId),
            new SqlParameter("@CategoryName", model.CategoryName),
            new SqlParameter("@Description", model.Description)
                };

                await _dbLayer.ExecuteSPAsync("sp_ManagePharamcycategory", parameters);

                TempData["Message"] = action == "Insert"
                    ? "Category added successfully!"
                    : "Category updated successfully!";

                TempData["MessageType"] = "success";
            }
            catch (SqlException ex)
            {
                // Handle RAISERROR from SP
                TempData["Message"] = ex.Message;
                TempData["MessageType"] = "error";

                return View(model); 
            }
            catch (Exception)
            {
                TempData["Message"] = "Something went wrong. Please try again.";
                TempData["MessageType"] = "error";
                return View(model);
            }

            return RedirectToAction("CategoryList");
        }

         
        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                await _dbLayer.ExecuteSPAsync("sp_ManagePharamcycategory", new[]
                {
            new SqlParameter("@Action", "Delete"),
            new SqlParameter("@CategoryId", id)
        });

                TempData["Message"] = "Category deleted successfully!";
                TempData["MessageType"] = "success";
            }
            catch
            {
                TempData["Message"] = "Unable to delete category.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("CategoryList");
        }


        [HttpPost]
        public async Task<IActionResult> DeleteSelectedCategory(int[] selectedIds)
        {
            try
            {
                if (selectedIds == null || selectedIds.Length == 0)
                {
                    TempData["Message"] = "Please select at least one category.";
                    TempData["MessageType"] = "warning";
                    return RedirectToAction("CategoryList");
                }

                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManagePharamcycategory", new SqlParameter[]
                    {
                new SqlParameter("@Action", "Delete"),
                new SqlParameter("@CategoryId", id)
                    });
                }

                TempData["Message"] = $"{selectedIds.Length} category(s) deleted successfully!";
                TempData["MessageType"] = "success";
            }
            catch (Exception)
            {
                TempData["Message"] = "Unable to delete selected category.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("CategoryList");
        }


        // ----------------- LIST -----------------
        [HttpGet]
        public async Task<IActionResult> MedicineList(string search = null, int? category = null)
        {
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Account", new { area = "" });
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "Select"),
                new SqlParameter("@FilterField", string.IsNullOrEmpty(search) ? DBNull.Value : (object)"productName"),
                new SqlParameter("@FilterValue", string.IsNullOrEmpty(search) ? DBNull.Value : (object)search)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageProduct", parameters);

            var medicines = dt.AsEnumerable().Select(r => new Medicine
            {
                Id = Convert.ToInt32(r["id"]),
                CId = r["CId"] != DBNull.Value ? Convert.ToInt32(r["CId"]) : (int?)null,
                CategoryName = r["CategoryName"].ToString(),
                ProductName = r["productName"].ToString(),
                Description = r["description"].ToString(),
                Qty = r["qty"] != DBNull.Value ? Convert.ToInt32(r["qty"]) : (int?)null,
                MRP = r["mrp"] != DBNull.Value ? Convert.ToDouble(r["mrp"]) : (double?)null,
                CP = r["cp"] != DBNull.Value ? Convert.ToDouble(r["cp"]) : (double?)null,
                Mfg = r["mfg"] != DBNull.Value ? Convert.ToDateTime(r["mfg"]) : (DateTime?)null,
                ExpiryDate = r["expirydate"] != DBNull.Value ? Convert.ToDateTime(r["expirydate"]) : (DateTime?)null
            }).ToList();

            return View(medicines);
        }

        // ----------------- ADD / EDIT -----------------
        [HttpGet]
        public async Task<IActionResult> AddMedicine(int? id)
        {
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Account", new { area = "" });
            var dtCat = await _dbLayer.ExecuteSPAsync("sp_ManagePharamcycategory", new SqlParameter[] { new SqlParameter("@Action", "Select") });
            ViewBag.Categories = dtCat.AsEnumerable().Select(r => new SelectListItem
            {
                Text = r["CategoryName"].ToString(),
                Value = r["CategoryId"].ToString()
            }).ToList();

            if (id == null)
                return View(new Medicine());

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "SelectById"),
                new SqlParameter("@Id", id)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageProduct", parameters);
            if (dt.Rows.Count == 0) return NotFound();

            var medicine = new Medicine
            {
                Id = Convert.ToInt32(dt.Rows[0]["id"]),
                CId = dt.Rows[0]["CId"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["CId"]) : (int?)null,
                CategoryName = dt.Rows[0]["CategoryName"].ToString(),
                ProductName = dt.Rows[0]["productName"].ToString(),
                Description = dt.Rows[0]["description"].ToString(),
                Qty = dt.Rows[0]["qty"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["qty"]) : (int?)null,
                MRP = dt.Rows[0]["mrp"] != DBNull.Value ? Convert.ToDouble(dt.Rows[0]["mrp"]) : (double?)null,
                CP = dt.Rows[0]["cp"] != DBNull.Value ? Convert.ToDouble(dt.Rows[0]["cp"]) : (double?)null,
                Mfg = dt.Rows[0]["mfg"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[0]["mfg"]) : (DateTime?)null,
                ExpiryDate = dt.Rows[0]["expirydate"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[0]["expirydate"]) : (DateTime?)null
            };

            return View(medicine);
        }

        [HttpPost]
        public async Task<IActionResult> AddMedicine(Medicine model)
        {
            // Load categories for dropdown
            var dtCat = await _dbLayer.ExecuteSPAsync("sp_ManagePharamcycategory",
                new SqlParameter[] { new SqlParameter("@Action", "Select") });

            ViewBag.Categories = dtCat.AsEnumerable().Select(r => new SelectListItem
            {
                Text = r["CategoryName"].ToString(),
                Value = r["CategoryId"].ToString()
            }).ToList();

            // Check model validation
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Please fill all required fields.";
                TempData["MessageType"] = "error";
                return View(model);
            }

            string action = model.Id > 0 ? "Update" : "Insert";

            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Action", action),
        new SqlParameter("@Id", model.Id),
        new SqlParameter("@CId", (object)model.CId ?? DBNull.Value),
        new SqlParameter("@ProductName", model.ProductName),
        new SqlParameter("@Description", model.Description ?? (object)DBNull.Value),
        new SqlParameter("@Qty", (object)model.Qty ?? DBNull.Value),
        new SqlParameter("@MRP", (object)model.MRP ?? DBNull.Value),
        new SqlParameter("@CP", (object)model.CP ?? DBNull.Value),
        new SqlParameter("@Mfg", (object)model.Mfg ?? DBNull.Value),
        new SqlParameter("@ExpiryDate", (object)model.ExpiryDate ?? DBNull.Value)
            };

            try
            {
                await _dbLayer.ExecuteSPAsync("sp_ManageProduct", parameters);

                TempData["Message"] = action == "Insert"
                    ? "Medicine added successfully!"
                    : "Medicine updated successfully!";
                TempData["MessageType"] = "success";

                return RedirectToAction("MedicineList");
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000) // RAISERROR from SP
                {
                    // Show error under ProductName field if duplicate
                    ModelState.AddModelError("ProductName", ex.Message);
                    TempData["Message"] = ex.Message;
                    TempData["MessageType"] = "error";
                    return View(model);
                }
                else
                {
                    throw; // Other SQL exceptions
                }
            }
        }

        // ----------------- DELETE -----------------
        [HttpPost]
        public async Task<IActionResult> DeleteMedicine(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_ManageProduct", new[]
            {
                new SqlParameter("@Action", "Delete"),
                new SqlParameter("@Id", id)
            });

            TempData["Message"] = "Medicine deleted successfully!";
            TempData["MessageType"] = "success";

            return RedirectToAction("MedicineList");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedMedicine(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManageProduct", new[]
                    {
                        new SqlParameter("@Action", "Delete"),
                        new SqlParameter("@Id", id)
                    });
                }

                TempData["Message"] = $"{selectedIds.Length} medicine(s) deleted successfully!";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "Please select at least one medicine.";
                TempData["MessageType"] = "warning";
            }

            return RedirectToAction("MedicineList");
        }



        // GET: Sale Medicine Page
        [HttpGet]
        public async Task<IActionResult> OtherSaleMedicine()
        {
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Account", new { area = "" });
            var model = new SaleMedicineViewModel();

            // 🔥 Generate next invoice number
            var dtInvoice = await _dbLayer.ExecuteSPAsync(
                "sp_ManageOtherMedicineSale",
                new[] { new SqlParameter("@Action", "GetNextInvoice") }
            );

            model.InvoiceId = dtInvoice.Rows[0]["InvoiceId"].ToString();
            model.SaleDate = DateTime.Now;

            // Load dropdowns
            await LoadDropdowns(model);

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> OtherSaleMedicine(SaleMedicineViewModel model)
        {
            ModelState.Remove("PatientId");
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
            new SqlParameter("@PatientName", model.PatientName ?? (object)DBNull.Value),
            new SqlParameter("@PatientNumber", model.PatientId),
            new SqlParameter("@InvoiceDate", model.SaleDate),
            new SqlParameter("@GrandTotal", model.GrandTotal),
            new SqlParameter("@DiscountPercent", model.DiscountPercent),
            new SqlParameter("@GstPercent", model.GstPercent),
            new SqlParameter("@FinalAmount", model.FinalAmount),
            new SqlParameter("@MedicinesJson", model.MedicinesJson)
        };

                await _dbLayer.ExecuteSPAsync("sp_ManageOtherMedicineSale", parameters);

                model.IsSaved = true;  

                TempData["Message"] = $"Sale {model.InvoiceId} saved successfully!";
                TempData["MessageType"] = "success";

                model.Medicines = new List<MedicineModel>();
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
        public async Task<IActionResult> PrintMedicineBillOther(string pageName, string id)
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



        [HttpGet]
        public async Task<IActionResult> OtherBillSaleMedicine(
  string filterColumn = null,
  string keyword = null,
  DateTime? fromDate = null,
  DateTime? toDate = null)
        {
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Account", new { area = "" });
            var dt = await _dbLayer.ExecuteSPAsync(
                "SP_SaleMedicineOtherReport",
                new[]
                {
            new SqlParameter("@FilterColumn", (object)filterColumn ?? DBNull.Value),
            new SqlParameter("@Keyword", (object)keyword ?? DBNull.Value),
            new SqlParameter("@FromDate", (object)fromDate ?? DBNull.Value),
            new SqlParameter("@ToDate", (object)toDate?.AddDays(1) ?? DBNull.Value)
                });

            var list = dt.AsEnumerable().Select(r => new SaleMedicineReportViewModel
            {
                Id = Convert.ToInt32(r["Id"]),
                InvoiceId = r["InvoiceId"].ToString(),
                InvoiceDate = Convert.ToDateTime(r["InvoiceDate"]),
                PatientId = r["InvoiceId"].ToString(),
                PatientName = r["PatientName"].ToString(),
                GrandTotal = Convert.ToDecimal(r["GrandTotal"]),
                FinalAmount = Convert.ToDecimal(r["FinalAmount"])
            }).ToList();

            return View(new SaleMedicineReportPageVM
            {
                FilterColumn = filterColumn,
                Keyword = keyword,
                FromDate = fromDate,
                ToDate = toDate,
                Records = list
            });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOtherMedicineBill(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Message"] = "Invalid invoice";
                TempData["MessageType"] = "error";
                return RedirectToAction("OtherBillSaleMedicine");
            }

            await _dbLayer.ExecuteSPAsync(
                "SP_SaleMedicineOtherReport",
                new[]
                {
            new SqlParameter("@Action", "DELETE"),
            new SqlParameter("@InvoiceId", id)
                });

            TempData["Message"] = "Invoice deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("OtherBillSaleMedicine");
        }


        [HttpPost]
        public async Task<IActionResult> DeleteSelectedOtherMedicine(string[] selectedIds)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData["Message"] = "No invoice selected";
                TempData["MessageType"] = "error";
                return RedirectToAction("OtherBillSaleMedicine");
            }

            foreach (var invoiceId in selectedIds)
            {
                await _dbLayer.ExecuteSPAsync(
                    "SP_SaleMedicineOtherReport",
                    new[]
                    {
                new SqlParameter("@Action", "DELETE"),
                new SqlParameter("@InvoiceId", invoiceId)
                    });
            }

            TempData["Message"] = "Selected invoices deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("OtherBillSaleMedicine");
        }












    }

}
