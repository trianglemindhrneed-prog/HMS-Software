using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HMSCore.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class IPDDetailsController : BaseController
    {
        private readonly IDbLayer _dbLayer;
        private readonly IConfiguration _configuration;
        public IPDDetailsController(IDbLayer dbLayer, IConfiguration configuration)
        {
            _dbLayer = dbLayer;
            _configuration = configuration;
        }

        private async Task LoadPatientDropdown(string selectedPatientId = null)
        {
            DataTable dt = await _dbLayer.ExecuteSPAsync(
                "sp_IPDVitals_Manage",
                new[]
                {
            new SqlParameter("@Action", "PATIENT_DROPDOWN")
                });

            // 🔴 safety: agar SP se kuch nahi aaya
            var list = new List<SelectListItem>
    {
        new SelectListItem
        {
            Value = "",
            Text = "-- Select Patient --"
        }
    };

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow r in dt.Rows)
                {
                    string patientId = r["PatientID"]?.ToString();

                    list.Add(new SelectListItem
                    {
                        Value = patientId,          // UH0001
                        Text = patientId,           // UH0001
                        Selected = !string.IsNullOrEmpty(selectedPatientId)
                                   && patientId == selectedPatientId
                    });
                }
            }

            ViewBag.PatientList = list;
        }


        [HttpGet]
        public async Task<IActionResult> IPDNursingTasksDetails(string status, string search)
        {
            string action = string.IsNullOrEmpty(search) ? "GETALL" : "SEARCH";

            var parameters = new[]
            {
        new SqlParameter("@Action", action),
        new SqlParameter("@SearchValue", (object?)search ?? DBNull.Value),
        new SqlParameter("@SearchField", (object?)status ?? DBNull.Value)
    };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_IPDNursingTasks_Manage", parameters);

            var list = dt.AsEnumerable().Select(row => new IPDNursingTaskViewModel
            {
                TaskId = Convert.ToInt32(row["TaskId"]),
                PatientId = row["PatientId"]?.ToString(),
                TaskName = row["TaskName"]?.ToString(),
                ScheduledDate = row["ScheduledDate"] == DBNull.Value ? null : Convert.ToDateTime(row["ScheduledDate"]),
                GivenDate = row["GivenDate"] == DBNull.Value ? null : Convert.ToDateTime(row["GivenDate"]),
                Status = row["Status"]?.ToString(),
                Remarks = row["Remarks"]?.ToString(),
                CreatedBy = row["CreatedBy"]?.ToString()
            }).ToList();

            ViewBag.Status = status;
            ViewBag.Search = search;

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> IPDNursingTasks(int? id)
        {
            var model = new IPDNursingTaskViewModel
            {
                ScheduledDate = DateTime.Now
            };

            if (id.HasValue)
            {
                var dt = await _dbLayer.ExecuteSPAsync("sp_IPDNursingTasks_Manage",
                    new[]
                    {
                new SqlParameter("@Action","GETBYID"),
                new SqlParameter("@TaskId", id.Value)
                    });

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];

                    model.TaskId = Convert.ToInt32(row["TaskId"]);
                    model.PatientId = row["PatientId"]?.ToString();
                    model.TaskName = row["TaskName"]?.ToString();
                    model.ScheduledDate = row["ScheduledDate"] == DBNull.Value ? null : Convert.ToDateTime(row["ScheduledDate"]);
                    model.GivenDate = row["GivenDate"] == DBNull.Value ? null : Convert.ToDateTime(row["GivenDate"]);
                    model.Status = row["Status"]?.ToString();
                    model.Remarks = row["Remarks"]?.ToString();
                    model.CreatedBy = row["CreatedBy"]?.ToString();
                }
            }
            await LoadPatientDropdown(model.PatientId);

            return View(model);
        }

      
        [HttpPost]
        public async Task<IActionResult>IPDNursingTasks(IPDNursingTaskViewModel model)
        {
            string action = model.TaskId == 0 ? "INSERT" : "UPDATE";

            var parameters = new[]
            {
        new SqlParameter("@Action", action),
        new SqlParameter("@TaskId", model.TaskId),
        new SqlParameter("@PatientId", (object?)model.PatientId ?? DBNull.Value),
        new SqlParameter("@TaskName", (object?)model.TaskName ?? DBNull.Value),
        new SqlParameter("@ScheduledDate", (object?)model.ScheduledDate ?? DBNull.Value),
        new SqlParameter("@GivenDate", (object?)model.GivenDate ?? DBNull.Value),
        new SqlParameter("@Status", (object?)model.Status ?? DBNull.Value),
        new SqlParameter("@Remarks", (object?)model.Remarks ?? DBNull.Value),
        new SqlParameter("@CreatedBy", "Admin")
    };

            await _dbLayer.ExecuteSPAsync("sp_IPDNursingTasks_Manage", parameters);

            TempData["Message"] = action == "INSERT"
                ? "Nursing Task saved successfully!"
                : "Nursing Task updated successfully!";

            TempData["MessageType"] = "success";

            return RedirectToAction("IPDNursingTasksDetails");
        }


        [HttpPost]
        public async Task<IActionResult> DeleteTask(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_IPDNursingTasks_Manage",
                new[]
                {
            new SqlParameter("@Action","DELETE"),
            new SqlParameter("@TaskId",id)
                });

            TempData["Message"] = "Task deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("IPDNursingTasksDetails");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedTask(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                string ids = string.Join(",", selectedIds);

                await _dbLayer.ExecuteSPAsync("sp_IPDNursingTasks_Manage", new[]
                {
            new SqlParameter("@Action", "DELETE_MULTIPLE"),
            new SqlParameter("@SearchValue", ids)
        });

                TempData["Message"] = "Selected tasks deleted successfully";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "No records selected";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("IPDNursingTasksDetails");
        }


        //=================IPDTRTPlanDetails====================

        [HttpGet]
        public async Task<IActionResult> IPDTRTPlanDetails(string column, string search)
        {
            string action = string.IsNullOrEmpty(search) ? "GETALL" : "SEARCH";

            var parameters = new[]
            {
        new SqlParameter("@Action", action),
        new SqlParameter("@FilterColumn", (object?)column ?? DBNull.Value),
        new SqlParameter("@FilterValue", (object?)search ?? DBNull.Value)
    };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_IPDTreatmentPlan_Manage", parameters);

            var list = dt.AsEnumerable().Select(row => new IPDTreatmentPlan
            {
                PlanId = Convert.ToInt32(row["PlanId"]),
                PatientId = row["PatientId"]?.ToString(),
                TreatmentDay = row["TreatmentDay"] == DBNull.Value ? null : Convert.ToInt32(row["TreatmentDay"]),
                TreatmentDate = Convert.ToDateTime(row["TreatmentDate"]),
                Diagnosis = row["Diagnosis"]?.ToString(),
                Treatment = row["Treatment"]?.ToString(),
                DietPlan = row["DietPlan"]?.ToString(),
                Notes = row["Notes"]?.ToString(),
                EnteredBy = row["EnteredBy"]?.ToString()
            }).ToList();

            ViewBag.Column = column;
            ViewBag.Search = search;

            return View(list);
        }



        [HttpGet]
        public async Task<IActionResult> IPDTreatmentPlan(int? id)
        {
            var model = new IPDTreatmentPlan
            {
                TreatmentDate = DateTime.Now
            };

            if (id.HasValue)
            {
                var dt = await _dbLayer.ExecuteSPAsync("sp_IPDTreatmentPlan_Manage",
                    new[]
                    {
                new SqlParameter("@Action","GETBYID"),
                new SqlParameter("@PlanId", id.Value)
                    });

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];

                    model.PlanId = Convert.ToInt32(row["PlanId"]);
                    model.PatientId = row["PatientId"]?.ToString();
                    model.TreatmentDay = row["TreatmentDay"] == DBNull.Value ? null : Convert.ToInt32(row["TreatmentDay"]);
                    model.TreatmentDate = Convert.ToDateTime(row["TreatmentDate"]);
                    model.Diagnosis = row["Diagnosis"]?.ToString();
                    model.Treatment = row["Treatment"]?.ToString();
                    model.DietPlan = row["DietPlan"]?.ToString();
                    model.Notes = row["Notes"]?.ToString();
                    model.EnteredBy = row["EnteredBy"]?.ToString();
                }
            }
            await LoadPatientDropdown(model.PatientId);
            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> IPDTreatmentPlan(IPDTreatmentPlan model)
        {
            string action = model.PlanId == 0 ? "INSERT" : "UPDATE";

            var parameters = new[]
            {
        new SqlParameter("@Action", action),
        new SqlParameter("@PlanId", model.PlanId),
        new SqlParameter("@PatientId", (object?)model.PatientId ?? DBNull.Value),
        new SqlParameter("@TreatmentDay", (object?)model.TreatmentDay ?? DBNull.Value),
        new SqlParameter("@TreatmentDate", model.TreatmentDate),
        new SqlParameter("@Diagnosis", (object?)model.Diagnosis ?? DBNull.Value),
        new SqlParameter("@Treatment", (object?)model.Treatment ?? DBNull.Value),
        new SqlParameter("@DietPlan", (object?)model.DietPlan ?? DBNull.Value),
        new SqlParameter("@Notes", (object?)model.Notes ?? DBNull.Value),
        new SqlParameter("@EnteredBy", "Admin")
    };

            await _dbLayer.ExecuteSPAsync("sp_IPDTreatmentPlan_Manage", parameters);

            TempData["Message"] = action == "INSERT"
                ? "Treatment Plan saved successfully!"
                : "Treatment Plan updated successfully!";

            TempData["MessageType"] = "success";

            return RedirectToAction("IPDTRTPlanDetails");
        }


        [HttpPost]
        public async Task<IActionResult> DeletePlan(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_IPDTreatmentPlan_Manage",
                new[]
                {
            new SqlParameter("@Action","DELETE"),
            new SqlParameter("@PlanId",id)
                });

            TempData["Message"] = "Treatment Plan deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("IPDTRTPlanDetails");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedPlan(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                string ids = string.Join(",", selectedIds);

                await _dbLayer.ExecuteSPAsync("sp_IPDTreatmentPlan_Manage",
                new[]
                {
            new SqlParameter("@Action","DELETE_MULTIPLE"),
            new SqlParameter("@FilterValue", ids)
                });

                TempData["Message"] = "Selected plans deleted successfully";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "No records selected";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("IPDTRTPlanDetails");
        }

    }
}
