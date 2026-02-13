using HMSCore.Areas.Admin.Controllers;
using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using HMSCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

[Area("Admin")]
public class IPDController : BaseController
{
    private readonly IDbLayer _dbLayer;
    private readonly IConfiguration _configuration;
    public IPDController(IDbLayer dbLayer, IConfiguration configuration)
    {
        _dbLayer = dbLayer;
        _configuration = configuration;
    }


    [HttpGet]
    public async Task<IActionResult> AddIPDAdmission()
    {
        IpdAdmission model = new();

        model.PatientID = await GetNextPatientID();
        model.AdmissionDateTime = DateTime.Now;
        model.Status = "Admitted";

        ViewBag.GenderList = GetGenderList();
        ViewBag.DoctorList = await GetDoctorsDropdown();
        ViewBag.BedCategoryList = await GetBedCategoryDropdown();
        ViewBag.Beds = new List<SelectListItem>();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddIPDAdmission(IpdAdmission model)
    {
        // 🔥 Force default values
        model.Status = "Admitted";
        model.AdmissionDateTime ??= DateTime.Now;

        // 🔥 Remove validation issues
        ModelState.Remove("DoctorId");
        ModelState.Remove("BedCategoryId");
        ModelState.Remove("BedId");
        ModelState.Remove("AdvanceAmount");
        ModelState.Remove("Age");

        if (!ModelState.IsValid)
        {
            await LoadDropdowns(model); // populate dropdowns
            return View(model);
        }

        // 🔹 Prepare SQL parameters
        SqlParameter[] param =
        {
        new SqlParameter("@Action", "INSERT"),
        new SqlParameter("@PatientID", model.PatientID),
        new SqlParameter("@DoctorId", model.DoctorId ?? (object)DBNull.Value),
        new SqlParameter("@BedCategoryId", model.BedCategoryId ?? (object)DBNull.Value),
        new SqlParameter("@BedId", model.BedId ?? (object)DBNull.Value),
        new SqlParameter("@AdmissionDateTime", model.AdmissionDateTime),
        new SqlParameter("@InitialDiagnosis", model.InitialDiagnosis ?? ""),
        new SqlParameter("@AdvanceAmount", model.AdvanceAmount ?? (object)DBNull.Value),
        new SqlParameter("@Name", model.Name ?? ""),
        new SqlParameter("@Age", model.Age ?? (object)DBNull.Value),
        new SqlParameter("@Gender", model.Gender ?? ""),
        new SqlParameter("@Number", model.Number ?? ""),
        new SqlParameter("@Status", "Admitted")
    };

        await _dbLayer.ExecuteSPAsync("sp_IPDAdmission_Manage", param);

        // 🔹 Only save message
        TempData["Message"] = $"Patient {model.PatientID} saved successfully!";
        TempData["MessageType"] = "success";
        return RedirectToAction("IPDPatients");
    }

    // Helper to populate dropdowns
    private async Task LoadDropdowns(IpdAdmission model)
    {
        ViewBag.GenderList = GetGenderList();
        ViewBag.DoctorList = await GetDoctorsDropdown();
        ViewBag.BedCategoryList = await GetBedCategoryDropdown();
        ViewBag.Beds = model.BedCategoryId.HasValue
            ? await GetBedsByCategory(model.BedCategoryId.Value)
            : new List<SelectListItem>();
    }

    [HttpGet]
    public async Task<IActionResult> GetBeds(int categoryId)
    {
        try
        {
            var list = new List<SelectListItem>();

            DataTable dt = await _dbLayer.ExecuteSPAsync(
                "sp_IPD_BedMaster",
                new[]
                {
                new SqlParameter("@Action","BedsByCategory"),
                new SqlParameter("@BedCategoryId", categoryId)
                });

            foreach (DataRow r in dt.Rows)
            {
                list.Add(new SelectListItem
                {
                    Value = r["BedId"].ToString(),
                    Text = r["BedNumber"].ToString()
                });
            }

            return Json(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }


    private async Task<List<SelectListItem>> GetBedsByCategory(int categoryId)
    {
        var list = new List<SelectListItem>();

        DataTable dt = await _dbLayer.ExecuteSPAsync(
            "sp_IPD_BedMaster",
            new[]
            {
            new SqlParameter("@Action","BedsByCategory"),
            new SqlParameter("@BedCategoryId", categoryId)
            });

        foreach (DataRow r in dt.Rows)
            list.Add(new SelectListItem
            {
                Value = r["BedId"].ToString(),
                Text = r["BedNumber"].ToString()
            });

        return list;
    }
    /* ================= LIST + SEARCH ================= */
    [HttpGet]
    public async Task<IActionResult> IPDPatients(string search = "", string filter = "")
    {
        // Call SP
        SqlParameter[] param =
        {
        new SqlParameter("@Action", "LIST"),
        new SqlParameter("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : search),
        new SqlParameter("@Status", DBNull.Value) // Status optional, can add later
    };

        DataTable dt = await _dbLayer.ExecuteSPAsync("sp_IPDAdmission_Manage", param);

        // Convert to list
        var list = dt.AsEnumerable().Select(r => new IpdAdmission
        {
            AdmissionID = Convert.ToInt32(r["AdmissionID"]),
            PatientID = r["PatientID"].ToString(),
            Name = r["Name"].ToString(),
            Number = r["Number"].ToString(),
            Age = r["Age"] == DBNull.Value ? 0 : Convert.ToInt32(r["Age"]),
            Gender = r["Gender"].ToString(),
            Status = r["Status"].ToString(),
            AdmissionDateTime = r["AdmissionDateTime"] == DBNull.Value
                ? (DateTime?)null
                : Convert.ToDateTime(r["AdmissionDateTime"])
        }).ToList();

        // Apply dynamic filter based on dropdown
        if (!string.IsNullOrWhiteSpace(search) && !string.IsNullOrWhiteSpace(filter))
        {
            switch (filter)
            {
                case "PatientName":
                    list = list.Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                    break;
                case "Gender":
                    list = list.Where(x => x.Gender.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                    break;
                case "Contact":
                    list = list.Where(x => x.Number.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                    break;
            }
        }

        return View(list.OrderBy(x => x.Name).ToList());
    }
    /* ================= DELETE ================= */
    //[HttpPost]
    //public async Task<IActionResult> ManageIPD(int id)
    //{
    //    await _dbLayer.ExecuteSPAsync(
    //        "sp_IPDAdmission_Manage",
    //        new[]
    //        {
    //            new SqlParameter("@Action","DELETE"),
    //            new SqlParameter("@AdmissionID",id)
    //        });

    //    TempData["Message"] = "Patient deleted successfully";
    //    TempData["MessageType"] = "success";
    //    return RedirectToAction("IPDPatients");
    //}

    //[HttpPost]
    //public async Task<IActionResult> DeleteSelected(int[] selectedIds)
    //{
    //    if (selectedIds != null && selectedIds.Length > 0)
    //    {
    //        foreach (int admissionId in selectedIds)
    //        {
    //            await _dbLayer.ExecuteSPAsync(
    //                "sp_IPDAdmission_Manage",
    //                new[]
    //                {
    //                new SqlParameter("@Action", "DELETE"),
    //                new SqlParameter("@AdmissionID", admissionId)
    //                }
    //            );
    //        }

    //        TempData["Message"] = "Selected IPD admissions deleted successfully";
    //        TempData["MessageType"] = "success";
    //    }
    //    else
    //    {
    //        TempData["Message"] = "No IPD admissions selected";
    //        TempData["MessageType"] = "error";
    //    }

    //    return RedirectToAction("IPDPatients"); // apna page naam
    //}

    /* ================= HELPERS ================= */
    private async Task<string> GetNextPatientID()
    {
        string q = "SELECT ISNULL(MAX(CAST(SUBSTRING(PatientID,3,10) AS INT)),0) FROM IPDAdmissions";
        int next = Convert.ToInt32(await _dbLayer.ExecuteScalarAsync(q)) + 1;
        return "UH" + next.ToString("D4");
    }

    private List<SelectListItem> GetGenderList() => new()
    {
        new SelectListItem{Text="-- Select Gender --",Value=""},
        new SelectListItem{Text="Male",Value="Male"},
        new SelectListItem{Text="Female",Value="Female"}
    };

    private async Task<List<SelectListItem>> GetDoctorsDropdown()
    {
        var list = new List<SelectListItem> { new("-- Select Doctor --", "") };

        DataTable dt = await _dbLayer.ExecuteSPAsync(
            "sp_ManageDoctor",
            new[] { new SqlParameter("@Action", "SelectDoctors") });

        foreach (DataRow r in dt.Rows)
            list.Add(new SelectListItem { Text = r["FullName"].ToString(), Value = r["DoctorId"].ToString() });

        return list;
    }

    private async Task<List<SelectListItem>> GetBedCategoryDropdown()
    {
        var list = new List<SelectListItem> { new("-- Select Bed Category --", "") };

        DataTable dt = await _dbLayer.ExecuteSPAsync(
            "sp_IPD_BedMaster",
            new[] { new SqlParameter("@Action", "BedCategory") });

        foreach (DataRow r in dt.Rows)
            list.Add(new SelectListItem { Text = r["BedCategory"].ToString(), Value = r["BedCategoryId"].ToString() });

        return list;
    }


    //========================Delete==============

    [HttpPost]
    public async Task<IActionResult> DeleteIPDPatient(int id)
    {
        await _dbLayer.ExecuteSPAsync(
            "sp_IPDAdmission_Manage",
            new[]
            {
            new SqlParameter("@Action", "DELETE"),
            new SqlParameter("@AdmissionID", id)
            });

        TempData["Message"] = "IPD patient deleted successfully";
        TempData["MessageType"] = "success";

        return RedirectToAction("IPDPatients");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelectedIPD(int[] selectedIds)
    {
        if (selectedIds != null && selectedIds.Length > 0)
        {
            foreach (var id in selectedIds)
            {
                await _dbLayer.ExecuteSPAsync(
                    "sp_IPDAdmission_Manage",
                    new[]
                    {
                    new SqlParameter("@Action", "DELETE"),
                    new SqlParameter("@AdmissionID", id)
                    });
            }

            TempData["Message"] = "Selected IPD patients deleted successfully";
            TempData["MessageType"] = "success";
        }
        else
        {
            TempData["Message"] = "No IPD patients selected";
            TempData["MessageType"] = "error";
        }

        return RedirectToAction("IPDPatients");
    }


    //========= IPDCheckupHistory ====================

    [HttpGet]
    public async Task<IActionResult> IPDCheckupHistory(string pid)
    {

        var model = new IPDCheckupHistoryViewModel
        {
            PatientId = pid
        };

        // 🔹 Patient Profile
        var dtPatient = await _dbLayer.ExecuteSPAsync(
            "sp_IPDCheckupHistorydetails",
            new[]
            {
                new SqlParameter("@Action", "GetPatientProfile"),
                new SqlParameter("@PatientId", pid)
            });

        if (dtPatient.Rows.Count > 0)
        {
            var r = dtPatient.Rows[0];
            model.FullName = r["name"].ToString();
            model.Gender = r["Gender"].ToString();
            model.Age = r["Age"].ToString();
         
            model.Number = r["Number"].ToString();
            //model.Address = r["Address1"].ToString();
            model.ProfilePath = r["ProfilePath"].ToString();
        }

        // 🔹 Checkups
        var dtCheckups = await _dbLayer.ExecuteSPAsync(
            "sp_IPDCheckupHistorydetails",
            new[]
            {
                new SqlParameter("@Action", "GetCheckupHistory"),
                new SqlParameter("@PatientId", pid)
            });

        foreach (DataRow row in dtCheckups.Rows)
        {
            var checkup = new CheckupVM
            {
                CheckupId = Convert.ToInt32(row["CheckupId"]),
                CheckupDate = Convert.ToDateTime(row["CheckupDate"]),
                DoctorName = row["DoctorName"].ToString(),
                Symptoms = row["Symptoms"].ToString(),
                Diagnosis = row["Diagnosis"].ToString(),
                ExtraNotes = row["ExtraNotes"].ToString()
            };

            // Prescriptions
            var dtPres = await _dbLayer.ExecuteSPAsync(
                "sp_IPDCheckupHistorydetails",
                new[]
                {
                    new SqlParameter("@Action", "GetPrescriptions"),
                    new SqlParameter("@CheckupId", checkup.CheckupId)
                });

            foreach (DataRow pr in dtPres.Rows)
            {
                checkup.Prescriptions.Add(new PrescriptionVM
                {
                    MedicineName = pr["MedicineName"].ToString(),
                    NoOfDays = pr["NoOfDays"].ToString(),
                    WhenToTake = pr["WhenToTake"].ToString(),
                    IsBeforeMeal = Convert.ToBoolean(pr["IsBeforeMeal"])
                });
            }

            model.Checkups.Add(checkup);
        }

        return View(model);
    }


    [HttpGet]
    public async Task<IActionResult> AddIPDNewCheckup(string pid)
    {
        if (string.IsNullOrWhiteSpace(pid))
        {
            TempData["Message"] = "Invalid patient.";
            TempData["MessageType"] = "error";
            return RedirectToAction("AddNewCheckup", "OPD");
        }

        var model = new AddIPDCheckupViewModel
        {
            PatientId = pid
        };

        // -------- Doctors --------
        var dtDoctors = await _dbLayer.ExecuteSPAsync(
            "sp_IPDManageCheckup",
            new[] { new SqlParameter("@Action", "GetDoctors") });

        foreach (DataRow r in dtDoctors.Rows)
        {
            model.Doctors.Add(new HMSCore.Areas.Admin.Models.Doctor
            {
                DoctorId = Convert.ToInt32(r["DoctorId"]),
                FullName = r["FullName"]?.ToString() ?? ""
            });
        }

        // -------- Medicines --------
        var dtMeds = await _dbLayer.ExecuteSPAsync(
            "sp_IPDManageCheckup",
            new[] { new SqlParameter("@Action", "GetMedicines") });

        foreach (DataRow r in dtMeds.Rows)
        {
            model.Medicines.Add(new MedicineVM
            {
                MedicineId = Convert.ToInt32(r["MedicineId"]),
                Name = r["MedicineName"]?.ToString() ?? ""
            });
        }

        return View(model);
    }


    // ================= ADD CHECKUP (POST) =================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddIPDNewCheckup(AddIPDCheckupViewModel model)
    {
        ModelState.Remove("MedicineName");
        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Please fill all required fields.";
            TempData["MessageType"] = "error";
            return RedirectToAction(nameof(AddIPDNewCheckup), new { pid = model.PatientId });
        }

        try
        {
            // -------- Insert Checkup --------
            var dt = await _dbLayer.ExecuteSPAsync(
                "sp_IPDManageCheckup",
                new[]
                {
                new SqlParameter("@Action", "InsertCheckup"),
                new SqlParameter("@PatientId", model.PatientId ?? ""),
                new SqlParameter("@DoctorId", model.SelectedDoctorId ?? 0),
                new SqlParameter("@Symptoms", model.Symptoms ?? ""),
                new SqlParameter("@Diagnosis", model.Diagnosis ?? ""),
                new SqlParameter("@CheckupDate", model.CheckupDate ?? (object)DBNull.Value),
                new SqlParameter("@ExtraNotes", model.ExtraNotes ?? (object)DBNull.Value)
                });

            int checkupId = Convert.ToInt32(dt.Rows[0]["NewCheckupId"]);

            // -------- Insert Prescriptions --------
            if (model.Prescriptions != null && model.Prescriptions.Count > 0)
            {
                foreach (var p in model.Prescriptions)
                {
                    await _dbLayer.ExecuteSPAsync(
                        "sp_IPDManageCheckup",
                        new[]
                        {
                        new SqlParameter("@Action", "InsertPrescription"),
                        new SqlParameter("@CheckupId", checkupId),
                        new SqlParameter("@MedicineId", p.MedicineId),
                        new SqlParameter("@NoOfDays", p.NoOfDays),
                        new SqlParameter("@WhenToTake", p.WhenToTake ?? (object)DBNull.Value),
                        new SqlParameter("@IsBeforeMeal", p.IsBeforeMeal)
                        });
                }
            }

            // -------- Success Message --------
            TempData["Message"] = $"Patient {model.PatientId} checkup added successfully!";
            TempData["MessageType"] = "success";

            return RedirectToAction("IPDCheckupHistory", "IPD", new { pid = model.PatientId });
        }
        catch
        {
            TempData["Message"] = "Something went wrong while saving checkup.";
            TempData["MessageType"] = "error";
            return RedirectToAction(nameof(AddIPDNewCheckup), new { pid = model.PatientId });
        }
    }


    // ================= Delete =================
    [HttpPost]
    public async Task<IActionResult> DeleteCheckup(int checkupId)
    {
        try
        {
            await _dbLayer.ExecuteSPAsync(
                "sp_IPDManageCheckupdetails",
                new[] {
                new SqlParameter("@Action", "DeleteCheckup"),
                new SqlParameter("@CheckupId", checkupId)
                });

            return Json(new { success = true, message = "Checkup deleted successfully!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error deleting checkup: " + ex.Message });
        }
    }


    // ================= ADD CHECKUP (GET) =================
    [HttpGet]
    public async Task<IActionResult> IPDEditCheckup(int checkupId)
    {
        var model = new AddIPDCheckupViewModel();

        // ---------- Load Checkup ----------
        var dt = await _dbLayer.ExecuteSPAsync(
            "sp_IPDManageCheckupdetails",
            new[]
            {
            new SqlParameter("@Action","GetCheckupById"),
            new SqlParameter("@CheckupId", checkupId)
            });

        if (dt.Rows.Count == 0)
        {
            TempData["Message"] = "Checkup not found.";
            TempData["MessageType"] = "error";
            return RedirectToAction("IPDCheckupHistory");
        }

        var r = dt.Rows[0];
        model.PatientId = r["PatientId"].ToString();
        model.SelectedDoctorId = Convert.ToInt32(r["DoctorId"]);
        model.Symptoms = r["Symptoms"]?.ToString();
        model.Diagnosis = r["Diagnosis"]?.ToString();
        model.CheckupDate = r["CheckupDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(r["CheckupDate"]);
        model.ExtraNotes = r["ExtraNotes"]?.ToString();

        ViewBag.CheckupId = checkupId;

        // ---------- Doctors ----------
        var dtDoctors = await _dbLayer.ExecuteSPAsync(
            "sp_IPDManageCheckupdetails",
            new[] { new SqlParameter("@Action", "GetDoctors") });

        foreach (DataRow d in dtDoctors.Rows)
            model.Doctors.Add(new HMSCore.Areas.Admin.Models.Doctor
            {
                DoctorId = Convert.ToInt32(d["DoctorId"]),
                FullName = d["FullName"].ToString()
            });

        // ---------- Medicines ----------
        var dtMeds = await _dbLayer.ExecuteSPAsync(
            "sp_IPDManageCheckupdetails",
            new[] { new SqlParameter("@Action", "GetMedicines") });

        foreach (DataRow m in dtMeds.Rows)
            model.Medicines.Add(new MedicineVM
            {
                MedicineId = Convert.ToInt32(m["MedicineId"]),
                Name = m["MedicineName"].ToString()
            });

        // ---------- Prescriptions ----------
        var dtPres = await _dbLayer.ExecuteSPAsync(
            "sp_IPDManageCheckupdetails",
            new[]
            {
            new SqlParameter("@Action","GetPrescriptions"),
            new SqlParameter("@CheckupId", checkupId)
            });

        foreach (DataRow p in dtPres.Rows)
            model.Prescriptions.Add(new PrescriptionVM
            {
                MedicineId = Convert.ToInt32(p["MedicineId"]),
                NoOfDays = p["NoOfDays"].ToString(),
                WhenToTake = p["WhenToTake"]?.ToString(),
                IsBeforeMeal = Convert.ToBoolean(p["IsBeforeMeal"])
            });

        return View(model);
    }


    // ================= ADD CHECKUP (POST) =================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IPDEditCheckup(AddIPDCheckupViewModel model, int checkupId)
    {
        // UPDATE CHECKUP
        await _dbLayer.ExecuteSPAsync(
            "sp_IPDManageCheckupdetails",
            new[]
            {
            new SqlParameter("@Action","UpdateCheckup"),
            new SqlParameter("@CheckupId",checkupId),
            new SqlParameter("@DoctorId",model.SelectedDoctorId),
            new SqlParameter("@Symptoms",model.Symptoms),
            new SqlParameter("@Diagnosis",model.Diagnosis),
            new SqlParameter("@CheckupDate",model.CheckupDate ?? (object)DBNull.Value),
            new SqlParameter("@ExtraNotes",model.ExtraNotes ?? (object)DBNull.Value)
            });

        // DELETE OLD PRESCRIPTIONS
        await _dbLayer.ExecuteSPAsync(
            "sp_IPDManageCheckupdetails",
            new[]
            {
            new SqlParameter("@Action","DeletePrescriptions"),
            new SqlParameter("@CheckupId",checkupId)
            });

        // INSERT NEW PRESCRIPTIONS
        foreach (var p in model.Prescriptions)
        {
            await _dbLayer.ExecuteSPAsync(
                "sp_IPDManageCheckupdetails",
                new[]
                {
                new SqlParameter("@Action","InsertPrescription"),
                new SqlParameter("@CheckupId",checkupId),
                new SqlParameter("@MedicineId",p.MedicineId),
                new SqlParameter("@NoOfDays",p.NoOfDays),
                new SqlParameter("@WhenToTake",p.WhenToTake ?? (object)DBNull.Value),
                new SqlParameter("@IsBeforeMeal",p.IsBeforeMeal)
                });
        }

        TempData["Message"] = "Checkup updated successfully!";
        TempData["MessageType"] = "success";

        return RedirectToAction("IPDCheckupHistory", "IPD", new { pid = model.PatientId });
    }
    //====================IpdCheckupInvoicePrint============
    [HttpGet]
    public async Task<IActionResult> PrintIpdCheckupInvoice(string pageName, string checkupId)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        using var client = new HttpClient(handler);

        var baseUrl = _configuration["WebFormBaseUrl"];
        var url = $"{baseUrl}/{pageName}.aspx?checkupId={checkupId}";

        var pdfBytes = await client.GetByteArrayAsync(url);

        Response.Headers.Add("Content-Disposition", $"inline; filename=Patient_{checkupId}.pdf");
        return File(pdfBytes, "application/pdf");
    }


    //=========================IpdVitalDetailsWork===================
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

  

    // ================= LIST ALL VITALS + SEARCH =================
    [HttpGet]
    public async Task<IActionResult> IpdVitalDetails(string SearchBy, string SearchValue)
    {
        List<IPDVital> list = new();

        SqlParameter[] param =
        {
        new SqlParameter("@Action", "GETALL"),
        new SqlParameter("@SearchBy",
            string.IsNullOrEmpty(SearchBy) ? DBNull.Value : (object)SearchBy),
        new SqlParameter("@SearchValue",
            string.IsNullOrEmpty(SearchValue) ? DBNull.Value : (object)SearchValue)
    };

        DataTable dt = await _dbLayer.ExecuteSPAsync(
            "sp_IPDVitals_Manage",
            param);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new IPDVital
            {
                VitalsId = Convert.ToInt32(r["VitalsId"]),
                PatientId = r["PatientId"].ToString(),
                Name = r["PatientName"]?.ToString(),
                RecordedDate = Convert.ToDateTime(r["RecordedDate"]),
                BloodGroup = r["BloodGroup"]?.ToString(),
                Temperature = r["Temperature"]?.ToString(),
                Pulse = r["Pulse"]?.ToString(),
                Height = r["Height"]?.ToString(),
                Weight = r["Weight"]?.ToString(),
                BP = r["BP"]?.ToString(),
                SpO2 = r["SpO2"]?.ToString(),
                RR = r["RR"]?.ToString(),
                Notes = r["Notes"]?.ToString(),
                RecordedBy = r["RecordedBy"]?.ToString()
            });
        }

        ViewBag.SearchBy = SearchBy;
        ViewBag.SearchValue = SearchValue;

        return View(list);
    }

    // ================= ADD / EDIT =================
    [HttpGet]
    public async Task<IActionResult> IpdVital(int? id)
    {
        IPDVital model = new()
        {
            RecordedDate = DateTime.Now
        };

        if (id != null)
        {
            DataTable dt = await _dbLayer.ExecuteSPAsync(
                "sp_IPDVitals_Manage",
                new SqlParameter[]
                {
                        new SqlParameter("@Action","GETBYID"),
                        new SqlParameter("@VitalsId", id)
                });

            if (dt.Rows.Count == 0)
                return NotFound();

            DataRow r = dt.Rows[0];

            model.VitalsId = Convert.ToInt32(r["VitalsId"]);
            model.PatientId = r["PatientId"].ToString();
            model.Temperature = r["Temperature"]?.ToString();
            model.Pulse = r["Pulse"]?.ToString();
            model.BP = r["BP"]?.ToString();
            model.SpO2 = r["SpO2"]?.ToString();
            model.RecordedDate = Convert.ToDateTime(r["RecordedDate"]);
            model.Notes = r["Notes"]?.ToString();
        }

        await LoadPatientDropdown(model.PatientId);
        return View(model);
    }

    // ================= SAVE (INSERT / UPDATE) =================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IpdVital(IPDVital model)
    {
        ModelState.Remove("RecordedBy");
        if (!ModelState.IsValid)
        {
            await LoadPatientDropdown(model.PatientId);
            return View(model);
        }

        try
        {
            string action = model.VitalsId > 0 ? "UPDATE" : "INSERT";

            SqlParameter[] param =
            {
            new SqlParameter("@Action", action),
            new SqlParameter("@VitalsId", model.VitalsId),
            new SqlParameter("@PatientId", model.PatientId),
            new SqlParameter("@RecordedDate", model.RecordedDate),
            new SqlParameter("@Temperature", (object)model.Temperature ?? DBNull.Value),
            new SqlParameter("@Pulse", (object)model.Pulse ?? DBNull.Value),
            new SqlParameter("@BP", (object)model.BP ?? DBNull.Value),
            new SqlParameter("@SpO2", (object)model.SpO2 ?? DBNull.Value),
            new SqlParameter("@RR", (object)model.RR ?? DBNull.Value),
            new SqlParameter("@Height", (object)model.Height ?? DBNull.Value),
            new SqlParameter("@Weight", (object)model.Weight ?? DBNull.Value),
            new SqlParameter("@BloodGroup", (object)model.BloodGroup ?? DBNull.Value),
            new SqlParameter("@Notes", (object)model.Notes ?? DBNull.Value),
            new SqlParameter("@RecordedBy", string.IsNullOrEmpty(model.RecordedBy) ? "Admin" : (object)model.RecordedBy),

     

        };

            await _dbLayer.ExecuteSPAsync("sp_IPDVitals_Manage", param);

            TempData["Message"] = model.VitalsId > 0
                ? "Vitals updated successfully"
                : "Vitals saved successfully";

            TempData["MessageType"] = "success";

            return RedirectToAction("IpdVitalDetails", new { pid = model.PatientId });
        }
        catch
        {
            TempData["Message"] = "Error while saving vitals";
            TempData["MessageType"] = "error";

            await LoadPatientDropdown(model.PatientId);
            return View(model);
        }
    }



    // ================= IPDPNDetailsList =================


    [HttpGet]
    public async Task<IActionResult> IPDPNDetails(string SearchValue)
    {
        List<IPDProgressNote> list = new();

        SqlParameter[] param =
        {
        new SqlParameter("@Action",
            string.IsNullOrEmpty(SearchValue) ? "GETALL" : "SEARCH"),

        new SqlParameter("@SearchText",
            string.IsNullOrEmpty(SearchValue) ? DBNull.Value : (object)SearchValue)
    };

        DataTable dt = await _dbLayer.ExecuteSPAsync(
            "sp_IPDProgressNotes_Manage",
            param);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new IPDProgressNote
            {
                NoteId = Convert.ToInt32(r["NoteId"]),
                PatientId = r["PatientId"]?.ToString(),
                VisitDate = r["VisitDate"] == DBNull.Value
                            ? DateTime.Now
                            : Convert.ToDateTime(r["VisitDate"]),
                DoctorId = r["DoctorId"] == DBNull.Value ? null : (int?)r["DoctorId"],
                DoctorName = r["DoctorName"] == DBNull.Value ? "" : r["DoctorName"].ToString(),
                Subjective = r["Subjective"]?.ToString(),
                Objective = r["Objective"]?.ToString(),
                Assessment = r["Assessment"]?.ToString(),
                Plans = r["Plans"]?.ToString()
            });
        }

        ViewBag.SearchValue = SearchValue;

        return View(list);
    }

    // ================= IPDPNDetailsDocterdropdown =================

    private async Task LoadDoctorDropdown(int? selectedDoctorId = null)
    {
        DataTable dt = await _dbLayer.ExecuteSPAsync(
            "sp_IPDProgressNotes_Manage",   // ✅ Correct SP
            new[]
            {
            new SqlParameter("@Action", "DOCTOR_DROPDOWN")
            });

        var list = new List<SelectListItem>
    {
        new SelectListItem
        {
            Value = "",
            Text = "-- Select Doctor --"
        }
    };

        if (dt != null && dt.Rows.Count > 0)
        {
            foreach (DataRow r in dt.Rows)
            {
                int doctorId = Convert.ToInt32(r["DoctorId"]);
                string fullName = r["FullName"]?.ToString();

                list.Add(new SelectListItem
                {
                    Value = doctorId.ToString(),      // ID save hoga
                    Text = fullName,                  // Name show hoga
                    Selected = selectedDoctorId.HasValue
                               && doctorId == selectedDoctorId.Value
                });
            }
        }

        ViewBag.DoctorList = list;   // ✅ Correct ViewBag
    }

    // ================= IPDPNDetailsSave =================
    [HttpGet]
    public async Task<IActionResult> IPDProgressNote(int? noteId)
    {
        var model = new IPDProgressNote();

        if (noteId.HasValue)
        {
            model.NoteId = noteId.Value;

            var dt = await _dbLayer.ExecuteSPAsync(
                "sp_IPDProgressNotes_Manage",
                new[]
                {
                new SqlParameter("@Action", "GETALL")
                });

            var row = dt.AsEnumerable()
                        .FirstOrDefault(r => Convert.ToInt32(r["NoteId"]) == noteId);

            if (row != null)
            {
                model.PatientId = row["PatientId"].ToString();
                model.VisitDate = Convert.ToDateTime(row["VisitDate"]);
                model.DoctorId = row["DoctorId"] == DBNull.Value ? null : (int?)row["DoctorId"];
                model.Subjective = row["Subjective"].ToString();
                model.Objective = row["Objective"].ToString();
                model.Assessment = row["Assessment"].ToString();
                model.Plans = row["Plans"].ToString();
            }
        }

        // 🔴 YAHI CALL KARNA HAI
        await LoadPatientDropdown(model.PatientId);

        await LoadDoctorDropdown(model.DoctorId);

        return View(model);
    }
    [HttpPost]
    public async Task<IActionResult> IPDProgressNote(IPDProgressNote model)
    {
        if (!ModelState.IsValid)
            return View(model);

        string action = model.NoteId == 0 ? "INSERT" : "UPDATE";

        var parameters = new[]
        {
        new SqlParameter("@Action", action),
        new SqlParameter("@NoteId", model.NoteId),
        new SqlParameter("@PatientId", model.PatientId),
        new SqlParameter("@VisitDate", model.VisitDate),
        new SqlParameter("@DoctorId", model.DoctorId ?? (object)DBNull.Value),
        new SqlParameter("@Subjective", model.Subjective ?? (object)DBNull.Value),
        new SqlParameter("@Objective", model.Objective ?? (object)DBNull.Value),
        new SqlParameter("@Assessment", model.Assessment ?? (object)DBNull.Value),
        new SqlParameter("@Plans", model.Plans ?? (object)DBNull.Value)
    };

        await _dbLayer.ExecuteSPAsync("sp_IPDProgressNotes_Manage", parameters);

        TempData["Message"] = action == "INSERT"
            ? "Progress Note saved successfully!"
            : "Progress Note updated successfully!";

        TempData["MessageType"] = "success";

        return RedirectToAction("IPDPNDetails");
    }

    // ================= IPDPNDetailsDelete =================
    [HttpPost]
    public async Task<IActionResult> DeleteNotes(int id)
    {
        await _dbLayer.ExecuteSPAsync("sp_IPDProgressNotes_Manage", new[]
        {
        new SqlParameter("@Action", "DELETE"),
        new SqlParameter("@NoteId", id)
    });

        TempData["Message"] = "Progress note deleted successfully";
        TempData["MessageType"] = "success";

        return RedirectToAction("IPDPNDetails");
    }


    [HttpPost]
    public async Task<IActionResult> DeleteSelecteddata(int[] selectedIds)
    {
        if (selectedIds != null && selectedIds.Length > 0)
        {
            string ids = string.Join(",", selectedIds);

            await _dbLayer.ExecuteSPAsync("sp_IPDProgressNotes_Manage", new[]
            {
            new SqlParameter("@Action", "MULTIPLE_DELETE"),
            new SqlParameter("@Ids", ids)
        });

            TempData["Message"] = "Selected progress notes deleted successfully";
            TempData["MessageType"] = "success";
        }
        else
        {
            TempData["Message"] = "No records selected";
            TempData["MessageType"] = "error";
        }

        return RedirectToAction("IPDPNDetails");
    }


    // ================= IPDBillingLedgerDetailsList =================

    [HttpGet]
    public async Task<IActionResult> IPDBillingLedgerDetails(string searchColumn, string searchValue)
    {
        List<IPDBillingLedgerModel> list = new();

        SqlParameter[] param =
        {
        new SqlParameter("@Action",
            string.IsNullOrEmpty(searchValue) ? "GETALL" : "SEARCH"),

        new SqlParameter("@SearchColumn",
            string.IsNullOrEmpty(searchColumn) ? DBNull.Value : (object)searchColumn),

        new SqlParameter("@SearchValue",
            string.IsNullOrEmpty(searchValue) ? DBNull.Value : (object)searchValue)
    };

        DataTable dt = await _dbLayer.ExecuteSPAsync(
            "sp_IPDBillingLedger_Manage",
            param);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new IPDBillingLedgerModel
            {
                LedgerId = Convert.ToInt32(r["LedgerId"]),
                PatientId = r["PatientId"]?.ToString(),

                BillingDate = r["BillingDate"] == DBNull.Value
                                ? DateTime.Now
                                : Convert.ToDateTime(r["BillingDate"]),

                ServiceType = r["ServiceType"]?.ToString(),
                Description = r["Description"]?.ToString(),

                Amount = r["Amount"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(r["Amount"]),

                Discount = r["Discount"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(r["Discount"]),

                IsPaid = r["IsPaid"] != DBNull.Value &&
                         Convert.ToBoolean(r["IsPaid"]),

                CreatedBy = r["CreatedBy"]?.ToString()
            });
        }

        ViewBag.SearchColumn = searchColumn;
        ViewBag.SearchValue = searchValue;

        return View(list);
    }

}

