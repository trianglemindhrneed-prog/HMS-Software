using HMSCore.Areas.Admin.Controllers;
using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

[Area("Admin")]
public class IPDController : BaseController
{
    private readonly IDbLayer _dbLayer;

    public IPDController(IDbLayer dbLayer)
    {
        _dbLayer = dbLayer;
    }

    /* ================= ADD / EDIT ================= */
    [HttpGet]
    public async Task<IActionResult> AddIPDAdmission(int? id)
    {
        IpdAdmission model = new();

        if (id != null)
        {
            DataTable dt = await _dbLayer.ExecuteSPAsync(
                "sp_IPDAdmission_Manage",
                new[]
                {
                    new SqlParameter("@Action","GETBYID"),
                    new SqlParameter("@AdmissionID",id)
                });

            if (dt.Rows.Count == 0) return NotFound();

            DataRow r = dt.Rows[0];
            model.AdmissionId = Convert.ToInt32(r["AdmissionID"]);
            model.PatientID = r["PatientID"].ToString();
            model.Name = r["Name"].ToString();
            model.Age = r["Age"] != DBNull.Value ? Convert.ToInt32(r["Age"]) : null;
            model.Gender = r["Gender"].ToString();
            model.Number = r["Number"].ToString();
            model.DoctorId = r["DoctorID"] != DBNull.Value ? Convert.ToInt32(r["DoctorID"]) : null;
            model.BedCategoryId = r["BedCategoryId"] != DBNull.Value ? Convert.ToInt32(r["BedCategoryId"]) : null;
            model.BedId = r["BedId"] != DBNull.Value ? Convert.ToInt32(r["BedId"]) : null;
            model.AdvanceAmount = r["AdvanceAmount"] != DBNull.Value ? Convert.ToDecimal(r["AdvanceAmount"]) : null;
            model.AdmissionDateTime = r["AdmissionDateTime"] != DBNull.Value
                ? Convert.ToDateTime(r["AdmissionDateTime"])
                : DateTime.Now;
            model.Status = r["Status"].ToString();
        }
        else
        {
            model.PatientID = await GetNextPatientID();
            model.AdmissionDateTime = DateTime.Now;
            model.Status = "Admitted";
        }

        ViewBag.GenderList = GetGenderList();
        ViewBag.DoctorList = await GetDoctorsDropdown();
        ViewBag.BedCategoryList = await GetBedCategoryDropdown();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddIPDAdmission(IpdAdmission model)
    {
        ViewBag.GenderList = GetGenderList();
        ViewBag.DoctorList = await GetDoctorsDropdown();
        ViewBag.BedCategoryList = await GetBedCategoryDropdown();

        if (!ModelState.IsValid) return View(model);

        string action = model.AdmissionId > 0 ? "UPDATE" : "INSERT";

        SqlParameter[] param =
        {
            new SqlParameter("@Action",action),
            new SqlParameter("@AdmissionID",model.AdmissionId),
            new SqlParameter("@PatientID",model.PatientID),
            new SqlParameter("@DoctorID",model.DoctorId ?? (object)DBNull.Value),
            new SqlParameter("@BedCategoryId",model.BedCategoryId ?? (object)DBNull.Value),
            new SqlParameter("@BedId",model.BedId ?? (object)DBNull.Value),
            new SqlParameter("@AdmissionDateTime",model.AdmissionDateTime),
            new SqlParameter("@InitialDiagnosis",model.InitialDiagnosis ?? ""),
            new SqlParameter("@AdvanceAmount",model.AdvanceAmount ?? (object)DBNull.Value),
            new SqlParameter("@Name",model.Name),
            new SqlParameter("@Age",model.Age ?? (object)DBNull.Value),
            new SqlParameter("@Gender",model.Gender),
            new SqlParameter("@Number",model.Number),
            new SqlParameter("@Status",model.Status)
        };



        await _dbLayer.ExecuteSPAsync("sp_IPDAdmission_Manage", param);

        TempData["Message"] = "IPD Admission Saved Successfully";
        TempData["MessageType"] = "success";

        return RedirectToAction("IPDPatients");


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
            AdmissionId = Convert.ToInt32(r["AdmissionID"]),
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
    [HttpPost]
    public async Task<IActionResult> ManageIPD(int id)
    {
        await _dbLayer.ExecuteSPAsync(
            "sp_IPDAdmission_Manage",
            new[]
            {
                new SqlParameter("@Action","DELETE"),
                new SqlParameter("@AdmissionID",id)
            });

        return RedirectToAction("IPDPatients");
    }

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


    //========= IPDCheckupHistory ====================
    public async Task<IActionResult> IPDCheckupHistory(string pid)
    {
        if (string.IsNullOrEmpty(pid))
            return RedirectToAction("IPDPatients");

        IPDCheckupHistoryPageVM model = new();

        /* ========= PATIENT PROFILE ========= */
        SqlParameter[] profileParam =
        {
        new SqlParameter("@Action", "PATIENT_PROFILE"),
        new SqlParameter("@PatientID", pid)   // ✅ FIXED
    };

        DataTable dtProfile =
            await _dbLayer.ExecuteSPAsync("sp_IPDCheckupMaster", profileParam);

        if (dtProfile.Rows.Count > 0)
        {
            var r = dtProfile.Rows[0];
            model.Patient.PatientID = r["PatientID"].ToString();
            model.Patient.Name = r["Name"].ToString();
            model.Patient.Age = r["Age"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["Age"]);
            model.Patient.Gender = r["Gender"].ToString();
            model.Patient.Number = r["Number"].ToString();
            model.Patient.Address = r["Address"].ToString();
            model.Patient.ProfilePath = r["ProfilePath"].ToString();
        }

        /* ========= CHECKUP HISTORY ========= */
        SqlParameter[] chkParam =
        {
        new SqlParameter("@Action", "CHECKUP_HISTORY"),
        new SqlParameter("@PatientID", pid)   // ✅ FIXED
    };

        DataTable dtCheckups =
            await _dbLayer.ExecuteSPAsync("sp_IPDCheckupMaster", chkParam);

        foreach (DataRow row in dtCheckups.Rows)
        {
            IPDCheckupVM chk = new()
            {
                CheckupId = Convert.ToInt32(row["CheckupId"]),
                CheckupDate = row["CheckupDate"] == DBNull.Value
                    ? null
                    : (DateTime?)Convert.ToDateTime(row["CheckupDate"]),
                DoctorName = row["DoctorName"].ToString(),
                Symptoms = row["Symptoms"].ToString(),
                Diagnosis = row["Diagnosis"].ToString(),
                ExtraNotes = row["ExtraNotes"].ToString()
            };

            /* ===== PRESCRIPTION ===== */
            SqlParameter[] preParam =
            {
            new SqlParameter("@Action", "PRESCRIPTION"),
            new SqlParameter("@CheckupId", chk.CheckupId)
        };

            DataTable dtPre =
                await _dbLayer.ExecuteSPAsync("sp_IPDCheckupMaster", preParam);

            foreach (DataRow p in dtPre.Rows)
            {
                chk.Prescriptions.Add(new IPDPrescriptionVM
                {
                    MedicineName = p["MedicineName"].ToString(),
                    NoOfDays = p["NoOfDays"].ToString(),
                    WhenToTake = p["WhenToTake"].ToString(),
                    IsBeforeMeal = Convert.ToBoolean(p["IsBeforeMeal"])
                });
            }

            model.Checkups.Add(chk);
        }

        return View(model);
    }
    // ===============================
    // DELETE CHECKUP (AJAX)
    // ===============================
    //[HttpPost]
    //public async Task<IActionResult> DeletePatients(int id)
    //{
    //    await _dbLayer.ExecuteSPAsync("sp_IPDCheckupMaster", new[]
    //    {
    //    new SqlParameter("@Action", "DeletePatient"),
    //    new SqlParameter("@PatientId", id)
    //});

    //    TempData["Message"] = "Patient deleted successfully";
    //    TempData["MessageType"] = "success";

    //    return RedirectToAction("IPDPatients");
    //}

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


}



