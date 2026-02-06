using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Globalization;

namespace HMSCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BedManagementController : BaseController
    {
        private readonly IDbLayer _dbLayer;
        private readonly IConfiguration _configuration;

        public BedManagementController(IDbLayer dbLayer, IConfiguration configuration)
        {
            _dbLayer = dbLayer;
            _configuration = configuration;
        }
        // ----------------- LIST -----------------
        [HttpGet]
        public async Task<IActionResult> BedList(string FilterField = null, string FilterValue = null)
        {
            SqlParameter[] parameters =
            {
        new SqlParameter("@Action", "Select"),
        new SqlParameter("@FilterField",
            string.IsNullOrEmpty(FilterField) ? DBNull.Value : (object)FilterField),
        new SqlParameter("@FilterValue",
            string.IsNullOrEmpty(FilterValue) ? DBNull.Value : (object)FilterValue)
    };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageBed", parameters);

            var beds = dt.AsEnumerable().Select(r => new Bed
            {
                BedId = Convert.ToInt32(r["BedId"]),
                BedCategoryId = Convert.ToInt32(r["BedCategoryId"]),
                CategoryName = r["CategoryName"].ToString(),
                BedNumber = r["BedNumber"].ToString(),
                Description = r["Description"].ToString()
            }).ToList();

            return View(beds);
        }

        // ----------------- ADD / EDIT -----------------
        [HttpGet] 
        public async Task<IActionResult> AddBed(int? id)
        {
            var dtCat = await _dbLayer.ExecuteSPAsync(
                "sp_ManageBedCategory",
                new[] { new SqlParameter("@Action", "Select") }
            );

            ViewBag.Categories = dtCat.AsEnumerable().Select(r => new SelectListItem
            {
                Text = r["BedCategoryName"].ToString(),
                Value = r["BedCategoryId"].ToString()
            }).ToList();

            if (id == null)
                return View(new Bed());

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageBed", new[]
            {
        new SqlParameter("@Action","SelectById"),
        new SqlParameter("@BedId",id)
    });

            if (dt.Rows.Count == 0) return NotFound();

            var bed = new Bed
            {
                BedId = Convert.ToInt32(dt.Rows[0]["BedId"]),
                BedCategoryId = Convert.ToInt32(dt.Rows[0]["BedCategoryId"]), 
                BedNumber = dt.Rows[0]["BedNumber"].ToString(),
                Description = dt.Rows[0]["Description"].ToString()
            };

            return View(bed);
        }

        [HttpPost]
        public async Task<IActionResult> AddBed(Bed model)
        {
            // 🔹 ALWAYS reload category dropdown
            var dtCat = await _dbLayer.ExecuteSPAsync(
                "sp_ManageBedCategory",
                new[] { new SqlParameter("@Action", "Select") }
            );

            ViewBag.Categories = dtCat.AsEnumerable().Select(r => new SelectListItem
            {
                Text = r["BedCategoryName"].ToString(),
                Value = r["BedCategoryId"].ToString(),
                Selected = (model.BedCategoryId != null &&
                            r["BedCategoryId"].ToString() == model.BedCategoryId.ToString())
            }).ToList();

            // 🔹 Model validation
            if (!ModelState.IsValid)
                return View(model);

            string action = model.BedId > 0 ? "Update" : "Insert";

            SqlParameter[] parameters =
            {
        new SqlParameter("@Action", action),
        new SqlParameter("@BedId", model.BedId),
        new SqlParameter("@BedCategoryId", model.BedCategoryId),
        new SqlParameter("@BedNumber", model.BedNumber),
        new SqlParameter("@Description", model.Description ?? (object)DBNull.Value)
    };

            try
            {
                await _dbLayer.ExecuteSPAsync("sp_ManageBed", parameters);

                TempData["Message"] = action == "Insert"
                    ? "Bed added successfully!"
                    : "Bed updated successfully!";
                TempData["MessageType"] = "success";

                return RedirectToAction("BedList");
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                // 🔴 Duplicate error from SP
                ModelState.AddModelError("BedNumber", ex.Message);
                return View(model); // dropdown + selected value preserved
            }
        }

        // ----------------- DELETE -----------------
        [HttpPost]
        public async Task<IActionResult> DeleteBed(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_ManageBed", new[]
            {
        new SqlParameter("@Action","Delete"),
        new SqlParameter("@BedId",id)
    });

            TempData["Message"] = "Bed deleted successfully!";
            TempData["MessageType"] = "success";

            return RedirectToAction("BedList");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedBed(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManageBed", new[]
                    {
                        new SqlParameter("@Action", "Delete"),
                        new SqlParameter("@BedId", id)
                    });
                }

                TempData["Message"] = $"{selectedIds.Length} bed(s) deleted successfully!";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "Please select at least one Bed.";
                TempData["MessageType"] = "warning";
            }

            return RedirectToAction("BedList");
        }

        [HttpGet]
        public async Task<IActionResult> BedCategoriesList(string search = null, int? status = null)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "Select"),
                new SqlParameter("@FilterVal", string.IsNullOrEmpty(search) ? DBNull.Value : search),
                new SqlParameter("@Status", status.HasValue ? status.Value : (object)DBNull.Value)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageBedCategory", parameters);

            var departments = dt.AsEnumerable().Select(r => new BedCategory
            {
                BedCategoryId = Convert.ToInt32(r["BedCategoryId"]),
                BedPrice = Convert.ToInt32(r["BedPrice"]),
                BedCategoryName = r["BedCategoryName"].ToString(),
                Description = r["Description"].ToString(),

            }).ToList();

            ViewData["Search"] = search;
            ViewData["Status"] = status;

            return View(departments);
        }
         
        [HttpGet]
        public async Task<IActionResult> AddBedCategories(int? id)
        {
            if (id == null)
            {
                // Adding a new category
                return View(new BedCategory());
            }

            // Editing existing category
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Action", "SelectBYId"),
        new SqlParameter("@BedCategoryId", id)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageBedCategory", parameters);

            if (dt.Rows.Count == 0) return NotFound();

            var category = new BedCategory
            {
                BedCategoryId = Convert.ToInt32(dt.Rows[0]["BedCategoryId"]),
                BedPrice = Convert.ToInt32(dt.Rows[0]["BedPrice"]),
                BedCategoryName = dt.Rows[0]["BedCategoryName"].ToString(),
                Description = dt.Rows[0]["Description"].ToString()
            };

            return View(category);
        }
         
        [HttpPost]
        public async Task<IActionResult> AddBedCategories(BedCategory model)
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
                string action = model.BedCategoryId > 0 ? "Update" : "Insert";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@Action", action),
            new SqlParameter("@BedCategoryId", model.BedCategoryId),
            new SqlParameter("@BedPrice", model.BedPrice),
            new SqlParameter("@BedCategoryName", model.BedCategoryName),
            new SqlParameter("@Description", model.Description)
                };

                await _dbLayer.ExecuteSPAsync("sp_ManageBedCategory", parameters);

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

            return RedirectToAction("BedCategoriesList");
        }
         
        [HttpPost]
        public async Task<IActionResult> DeleteBedCategory(int id)
        {
            try
            {
                await _dbLayer.ExecuteSPAsync("sp_ManageBedCategory", new[]
                {
            new SqlParameter("@Action", "Delete"),
            new SqlParameter("@BedCategoryId", id)
        });

                TempData["Message"] = "Category deleted successfully!";
                TempData["MessageType"] = "success";
            }
            catch
            {
                TempData["Message"] = "Unable to delete category.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("BedCategoriesList");
        }
         
        [HttpPost]
        public async Task<IActionResult> DeleteSelectedBedCategory(int[] selectedIds)
        {
            try
            {
                if (selectedIds == null || selectedIds.Length == 0)
                {
                    TempData["Message"] = "Please select at least one category.";
                    TempData["MessageType"] = "warning";
                    return RedirectToAction("BedCategoriesList");
                }

                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManageBedCategory", new SqlParameter[]
                    {
                new SqlParameter("@Action", "Delete"),
                new SqlParameter("@BedCategoryId", id)
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

            return RedirectToAction("BedCategoriesList");
        }

        [HttpGet]
        public async Task<IActionResult> BedAllotmentList(
          string filterColumn = null,
          string keyword = null,
          string fromDate = null,
          string toDate = null)
        {
            // Parse dates safely
            object fromDateValue = string.IsNullOrEmpty(fromDate)
                ? DBNull.Value
                : (object)DateTime.Parse(fromDate, CultureInfo.InvariantCulture);

            object toDateValue = string.IsNullOrEmpty(toDate)
                ? DBNull.Value
                : (object)DateTime.Parse(toDate, CultureInfo.InvariantCulture);

            // SP parameters
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Action", "Select"),
        new SqlParameter("@FilterColumn", string.IsNullOrEmpty(filterColumn) ? DBNull.Value : (object)filterColumn),
        new SqlParameter("@Keyword", string.IsNullOrEmpty(keyword) ? DBNull.Value : (object)keyword),
        new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDateValue },
        new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = toDateValue }
            };

            // Execute SP
            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageBedAllotment", parameters);

            // Map results
            var bedAllotments = dt.AsEnumerable().Select(r => new BedAllotmentViewModel
            {
                BedAllotmentId = r["BedAllotmentId"] == DBNull.Value ? 0 : Convert.ToInt32(r["BedAllotmentId"]),
                BedCategory = r["BedCategory"] == DBNull.Value ? "" : r["BedCategory"].ToString(),
                BedNumber = r["BedNumber"] == DBNull.Value ? "" : r["BedNumber"].ToString(),
                PatientId = r["PatientId"] == DBNull.Value ? "" : r["PatientId"].ToString(),
                PatientName = r["PatientName"] == DBNull.Value ? "" : r["PatientName"].ToString(),
                AllotmentDate = r["AllotmentDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["AllotmentDate"]),
                DischargeDate = r["DischargeDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["DischargeDate"])
            }).ToList();

            // Prepare ViewModel (ensure list is not null)
            var vm = new BedAllotmentListViewModel
            {
                BedAllotments = bedAllotments ?? new List<BedAllotmentViewModel>(),
                FilterColumn = filterColumn,
                Keyword = keyword,
                FromDate = string.IsNullOrEmpty(fromDate) ? (DateTime?)null : DateTime.Parse(fromDate),
                ToDate = string.IsNullOrEmpty(toDate) ? (DateTime?)null : DateTime.Parse(toDate)
            };

            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> DeleteBedAllotment(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_ManageBedAllotment", new[]
            {
                new SqlParameter("@Action", "Delete"),
                new SqlParameter("@BedAllotmentId", id)
            });

            TempData["Message"] = "Bed Allotment deleted successfully";
            TempData["MessageType"] = "success";
            return RedirectToAction("BedAllotmentList");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedBedAllotment(string selectedIds)
        {
            if (!string.IsNullOrEmpty(selectedIds))
            {
                var ids = selectedIds.Split(',').Select(int.Parse);
                foreach (var id in ids)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManageBedAllotment", new[]
                    {
                        new SqlParameter("@Action", "Delete"),
                        new SqlParameter("@BedAllotmentId", id)
                    });
                }

                TempData["Message"] = "Selected Bed Allotments deleted successfully";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "No Bed Allotments selected";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("BedAllotmentList");
        }



    }
}
