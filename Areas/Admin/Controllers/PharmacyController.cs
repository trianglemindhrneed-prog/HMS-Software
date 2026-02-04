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

        // POST: Save Medicine Sale
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
                // Convert medicines list to JSON for SP
                string jsonMedicines = Newtonsoft.Json.JsonConvert.SerializeObject(model.Medicines);

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
                    new SqlParameter("@MedicinesJson", jsonMedicines)
                };

                await _dbLayer.ExecuteSPAsync("sp_MedicineSale", parameters);

                model.IsSaved = true;
                TempData["Message"] = $"Sale {model.InvoiceId} saved successfully!";
                TempData["MessageType"] = "success";

                // Clear medicines for next sale
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
            var medicines = new List<MedicineModel>();

            var dt = await _dbLayer.ExecuteSPAsync(
                "sp_MedicineSale",
                new[] {
            new SqlParameter("@Action","GetMedicinesByCheckup"),
            new SqlParameter("@CheckupId", checkupId)
                }
            );
            foreach (DataRow row in dt.Rows)
            {
                int noOfDays = 0;
                int whenToTake = 0;
                int.TryParse(row["NoOfDays"]?.ToString(), out noOfDays);
                int.TryParse(row["WhenToTake"]?.ToString(), out whenToTake);
                int requestedQty = noOfDays * whenToTake;

                medicines.Add(new MedicineModel
                {
                    MedicineId = Convert.ToInt32(row["MedicineId"]),
                    MedicineName = row["MedicineName"].ToString(),
                    CategoryName = row["CategoryName"].ToString(),
                    MRP = Convert.ToDecimal(row["MRP"]),
                    AvailableQty = Convert.ToInt32(row["AvailableQty"]),
                    NoOfDays = noOfDays,
                    WhenToTake = whenToTake,
                    RequestedQty = requestedQty,
                    Quantity = requestedQty,
                    Total = Convert.ToDecimal(row["MRP"]) * requestedQty
                });
            }



            return Json(medicines);
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


    }

}
