using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace HMSCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LabManagementController : BaseController
    {
        private readonly IDbLayer _dbLayer;
        private readonly IConfiguration _configuration;
        public LabManagementController(IDbLayer dbLayer, IConfiguration configuration)
        {
            _dbLayer = dbLayer;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> LabCategoryList(string search = null, int? status = null)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "Select"),
                new SqlParameter("@FilterVal", string.IsNullOrEmpty(search) ? DBNull.Value : search),
                new SqlParameter("@Status", status.HasValue ? status.Value : (object)DBNull.Value)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageLabCategory", parameters);

            var departments = dt.AsEnumerable().Select(r => new LabCategory
            {
                LabCategoryId = Convert.ToInt32(r["LabCategoryId"]),
                CategoryName = r["CategoryName"].ToString(),
                Description = r["Description"].ToString(),

            }).ToList();

            ViewData["Search"] = search;
            ViewData["Status"] = status;

            return View(departments);
        }
         
        [HttpGet]
        public async Task<IActionResult> AddLabCategory(int? id)
        {
            if (id == null)
            {
                // Adding a new category
                return View(new LabCategory());
            }

            // Editing existing category
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Action", "SelectBYId"),
        new SqlParameter("@LabCategoryId", id)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageLabCategory", parameters);

            if (dt.Rows.Count == 0) return NotFound();

            var category = new LabCategory
            {
                LabCategoryId = Convert.ToInt32(dt.Rows[0]["LabCategoryId"]),
                CategoryName = dt.Rows[0]["CategoryName"].ToString(),
                Description = dt.Rows[0]["Description"].ToString()
            };

            return View(category);
        }
         
        [HttpPost]
        public async Task<IActionResult> AddLabCategory(LabCategory model)
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
                string action = model.LabCategoryId > 0 ? "Update" : "Insert";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@Action", action),
            new SqlParameter("@LabCategoryId", model.LabCategoryId),
            new SqlParameter("@LabCategory", model.CategoryName),
            new SqlParameter("@Description", model.Description)
                };

                await _dbLayer.ExecuteSPAsync("sp_ManageLabCategory", parameters);

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

            return RedirectToAction("LabCategoryList");
        }
         
        [HttpPost]
        public async Task<IActionResult> DeleteLabCategory(int id)
        {
            try
            {
                await _dbLayer.ExecuteSPAsync("sp_ManageLabCategory", new[]
                {
            new SqlParameter("@Action", "Delete"),
            new SqlParameter("@LabCategoryId", id)
        });

                TempData["Message"] = "Category deleted successfully!";
                TempData["MessageType"] = "success";
            }
            catch
            {
                TempData["Message"] = "Unable to delete category.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("LabCategoryList");
        }
         
        [HttpPost]
        public async Task<IActionResult> DeleteSelectedLabCategory(int[] selectedIds)
        {
            try
            {
                if (selectedIds == null || selectedIds.Length == 0)
                {
                    TempData["Message"] = "Please select at least one category.";
                    TempData["MessageType"] = "warning";
                    return RedirectToAction("LabCategoryList");
                }

                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManageLabCategory", new SqlParameter[]
                    {
                new SqlParameter("@Action", "Delete"),
                new SqlParameter("@LabCategoryId", id)
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

            return RedirectToAction("LabCategoryList");
        }
         
        // ----------------- LIST -----------------
        [HttpGet]
        public async Task<IActionResult> LabTestList(string search = null, string filterColumn = null)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Action", "Select"),
        new SqlParameter("@FilterColumn",
            string.IsNullOrEmpty(filterColumn) ? DBNull.Value : (object)filterColumn),
        new SqlParameter("@FilterValue",
            string.IsNullOrEmpty(search) ? DBNull.Value : (object)search)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageLabTest", parameters);

            var tests = dt.AsEnumerable().Select(r => new LabTest
            {
                LabId = Convert.ToInt32(r["LabId"]),
                LabCategoryId = r["LabCategoryId"] != DBNull.Value
                                ? Convert.ToInt32(r["LabCategoryId"])
                                : (int?)null,
                CategoryName = r["CategoryName"].ToString(),
                LabTestName = r["LabTestName"].ToString(),
                Description = r["Description"]?.ToString(),
                Unit = r["Unit"]?.ToString(),
                RefrenceManager = r["RefrenceManager"]?.ToString(),
                TestPrice = r["TestPrice"]?.ToString()
            }).ToList();

            ViewBag.Search = search;
            ViewBag.FilterColumn = filterColumn;

            return View(tests);
        }
         
        // ----------------- ADD / EDIT -----------------
        [HttpGet]
        public async Task<IActionResult> AddLabTest(int? id)
        {
            var dtCat = await _dbLayer.ExecuteSPAsync("sp_ManageLabCategory", new SqlParameter[] { new SqlParameter("@Action", "Select") });
            ViewBag.Categories = dtCat.AsEnumerable().Select(r => new SelectListItem
            {
                Text = r["CategoryName"].ToString(),
                Value = r["LabCategoryId"].ToString()
            }).ToList();

            if (id == null)
                return View(new LabTest());

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "SelectById"),
                new SqlParameter("@LabId", id)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageLabTest", parameters);
            if (dt.Rows.Count == 0) return NotFound();

            var test = new LabTest
            {
                LabId = Convert.ToInt32(dt.Rows[0]["LabId"]),
                LabCategoryId = Convert.ToInt32(dt.Rows[0]["LabCategoryId"]),
                LabTestName = dt.Rows[0]["LabTestName"].ToString(),
                Description = dt.Rows[0]["Description"].ToString(),
                Unit = dt.Rows[0]["Unit"].ToString(),
                RefrenceManager = dt.Rows[0]["RefrenceManager"].ToString(),
                TestPrice = dt.Rows[0]["TestPrice"].ToString()
            };

            return View(test);
        }

        [HttpPost]
        public async Task<IActionResult> AddLabTest(LabTest model)
        {
            // Load categories for dropdown
            var dtCat = await _dbLayer.ExecuteSPAsync("sp_ManageLabCategory",
                new SqlParameter[] { new SqlParameter("@Action", "Select") });

            ViewBag.Categories = dtCat.AsEnumerable().Select(r => new SelectListItem
            {
                Text = r["CategoryName"].ToString(),
                Value = r["LabCategoryId"].ToString()
            }).ToList();

            // Check model validation
            ModelState.Remove("CategoryName");
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Please fill all required fields.";
                TempData["MessageType"] = "error";
                return View(model);
            }

            string action = model.LabId > 0 ? "Update" : "Insert";

            SqlParameter[] parameters = new SqlParameter[]
    {
        new SqlParameter("@Action", action),
        new SqlParameter("@LabId", model.LabId),
        new SqlParameter("@LabCategoryId", model.LabCategoryId),
        new SqlParameter("@LabTestName", model.LabTestName),
        new SqlParameter("@Description", (object)model.Description ?? DBNull.Value),
        new SqlParameter("@Unit", (object)model.Unit ?? DBNull.Value),
        new SqlParameter("@RefrenceManager", (object)model.RefrenceManager ?? DBNull.Value),
        new SqlParameter("@TestPrice", (object)model.TestPrice ?? DBNull.Value)
    };

            try
            {
                await _dbLayer.ExecuteSPAsync("sp_ManageLabTest", parameters);

                TempData["Message"] = action == "Insert"
                    ? "Lab Test added successfully!"
                    : "Lab Test updated successfully!";
                TempData["MessageType"] = "success";

                return RedirectToAction("LabTestList");
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
        public async Task<IActionResult> DeleteLabTest(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_ManageLabTest", new[]
            {
                new SqlParameter("@Action", "Delete"),
                new SqlParameter("@LabId", id)
            });

            TempData["Message"] = "Lab Test deleted successfully!";
            TempData["MessageType"] = "success";

            return RedirectToAction("LabTestList");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedLabTest(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManageLabTest", new[]
                    {
                        new SqlParameter("@Action", "Delete"),
                        new SqlParameter("@LabId", id)
                    });
                }

                TempData["Message"] = $"{selectedIds.Length} LabTest(s) deleted successfully!";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "Please select at least one Lab Test.";
                TempData["MessageType"] = "warning";
            }

            return RedirectToAction("LabTestList");
        }

 
        // GET: PatientTest List
        public async Task<IActionResult> PatientsTest(string search = null, string filter = null)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "Select"),
                new SqlParameter("@FilterField", string.IsNullOrEmpty(filter) ? DBNull.Value : (object)filter),
                new SqlParameter("@FilterValue", string.IsNullOrEmpty(search) ? DBNull.Value : (object)search)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManagePatientTest", parameters);

            var model = dt.AsEnumerable().Select(r => new PatientTestViewModel
            {
                PatientTestId = Convert.ToInt32(r["PatientTestId"]),
                PatientId = r["PatientId"].ToString(),
                UserPatientsId = r["UserPatientsId"].ToString(),
                PatientName = r["PatientName"].ToString(),
                LabTestName = r["LabTestName"].ToString(),
                TestDate = r["TestDate"] != DBNull.Value ? (DateTime?)r["TestDate"] : null,
                DeliveryDate = r["DeliveryDate"] != DBNull.Value ? (DateTime?)r["DeliveryDate"] : null,
                ReportStatus = Convert.ToInt32(r["ReportStatus"]),
                ReportPath = r["ReportPath"].ToString()
            }).ToList();

            ViewBag.Filter = filter;
            ViewBag.Search = search;

            return View(model);
        }

        // POST: Delete Single
        [HttpPost]
        public async Task<IActionResult> DeletePatient(int id)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "Delete"),
                new SqlParameter("@PatientTestId", id)
            };

            await _dbLayer.ExecuteSPAsync("sp_ManagePatientTest", parameters);

            TempData["Message"] = "Test deleted successfully.";
            TempData["MessageType"] = "success";
            return RedirectToAction("PatientsTest");
        }

        // POST: Delete Multiple
        [HttpPost]
        public async Task<IActionResult> DeleteSelectedPatient(int[] selectedIds)
        {
            foreach (var id in selectedIds)
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@Action", "Delete"),
                    new SqlParameter("@PatientTestId", id)
                };
                await _dbLayer.ExecuteSPAsync("sp_ManagePatientTest", parameters);
            }

            TempData["Message"] = "Selected tests deleted successfully.";
            TempData["MessageType"] = "success";
            return RedirectToAction("PatientsTest");
        }

        [HttpGet]
        public async Task<IActionResult> PrintPatientsDetails(string pageName, string PatientTestId)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler);

            var baseUrl = _configuration["WebFormBaseUrl"];
            var url = $"{baseUrl}/{pageName}.aspx?PatientTestId={PatientTestId}";

            var pdfBytes = await client.GetByteArrayAsync(url);

            Response.Headers.Add("Content-Disposition", "inline; filename=Bill_{PatientTestId}.pdf");
            return File(pdfBytes, "application/pdf");
        }





        // GET: Add / Edit
        public async Task<IActionResult> AddPatientsTest(int? id)
        {
            var model = new PatientTestViewModel();
            await LoadDropdownsAsync();

            if (id.HasValue)
            {
                model = await GetPatientTestByIdAsync(id.Value);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPatientsTest(PatientTestViewModel model)
        {
            await LoadDropdownsAsync();

            if (model.Tests != null)
            {
                foreach (var t in model.Tests)
                {
                    if (t.LabTestId == 0)
                    {
                        ModelState.AddModelError("", "All Test rows must have a selected Test.");
                        return View(model);
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Determine Action
                string actionType = model.PatientTestId == 0 ? "SAVE" : "UPDATE";

                // Convert Test Details to CSV
                var sb = new StringBuilder();
                foreach (var t in model.Tests)
                {
                    sb.Append($"{t.LabTestId}|{t.Result ?? ""}|{t.Remarks ?? ""},");
                }
                string testDetailsCsv = sb.ToString().TrimEnd(',');

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@Action", actionType),
            new SqlParameter("@PatientTestId", model.PatientTestId),
            new SqlParameter("@PatientId", model.PatientId ?? (object)DBNull.Value),
            new SqlParameter("@ConsultantId", model.ConsultantId),
            new SqlParameter("@TestDate", model.TestDate ?? (object)DBNull.Value),
            new SqlParameter("@DeliveryDate", model.DeliveryDate ?? (object)DBNull.Value),
            new SqlParameter("@ReportStatus", model.ReportStatus),
            new SqlParameter("@TestDetailsCsv", testDetailsCsv)
                };

                await _dbLayer.ExecuteSPAsync("sp_ManageSaveUpdatePatientTest", parameters);
                 
                if (actionType == "SAVE")
                {
                    TempData["Message"] = "Patient Test saved successfully.";
                }
                else
                {
                    TempData["Message"] = "Patient Test updated successfully.";
                }

                TempData["MessageType"] = "success";

                return RedirectToAction("PatientsTest");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View(model);
            }
        }

        private async Task LoadDropdownsAsync()
        {
            // Patients
            var dtPatients = await _dbLayer.ExecuteSPAsync("sp_ManageSaveUpdatePatientTest",
                new SqlParameter[] { new SqlParameter("@Action", "GETDROPDOWNS_PATIENTS") });
            ViewBag.Patients = dtPatients.AsEnumerable()
                .Select(r => new DropDownItems { Id = Convert.ToInt32(r["Id"]), Name = r["Name"].ToString() })
                .ToList();

            // Consultants
            var dtConsultants = await _dbLayer.ExecuteSPAsync("sp_ManageSaveUpdatePatientTest",
                new SqlParameter[] { new SqlParameter("@Action", "GETDROPDOWNS_CONSULTANTS") });
            ViewBag.Consultants = dtConsultants.AsEnumerable()
                .Select(r => new DropDownItems { Id = Convert.ToInt32(r["Id"]), Name = r["Name"].ToString() })
                .ToList();

            // Lab Tests
            var dtLabTests = await _dbLayer.ExecuteSPAsync("sp_ManageSaveUpdatePatientTest",
                new SqlParameter[] { new SqlParameter("@Action", "GETDROPDOWNS_LABTESTS") });
            ViewBag.LabTests = dtLabTests.AsEnumerable()
                .Select(r => new DropDownItems { Id = Convert.ToInt32(r["Id"]), Name = r["Name"].ToString() })
                .ToList();
        }

        private async Task<PatientTestViewModel> GetPatientTestByIdAsync(int id)
        {
            var model = new PatientTestViewModel();

            SqlParameter[] parameters = new SqlParameter[]
            {
            new SqlParameter("@Action", "GETBYID"),
            new SqlParameter("@PatientTestId", id)
            };

            DataSet ds = await _dbLayer.ExecuteSPWithMultipleResultsAsync("sp_ManageSaveUpdatePatientTest", parameters);

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                var row = ds.Tables[0].Rows[0];
                model.PatientTestId = Convert.ToInt32(row["PatientTestId"]);
                model.PatientId = row["PatientId"].ToString();
                model.ConsultantId = Convert.ToInt32(row["ConsultantId"]);
                model.TestDate = row["TestDate"] != DBNull.Value ? (DateTime?)row["TestDate"] : null;
                model.DeliveryDate = row["DeliveryDate"] != DBNull.Value ? (DateTime?)row["DeliveryDate"] : null;
                model.ReportStatus = Convert.ToInt32(row["ReportStatus"]);
            }

            if (ds.Tables.Count > 1)
            {
                foreach (DataRow dr in ds.Tables[1].Rows)
                {
                    model.Tests.Add(new TestItemViewModel
                    {
                        LabTestId = Convert.ToInt32(dr["LabTestId"]),
                        Result = dr["Result"].ToString(),
                        Remarks = dr["Remarks"].ToString()
                    });
                }
            }

            return model;
        }










    }
}
