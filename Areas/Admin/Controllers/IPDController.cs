using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using HMSCore.Areas.Admin.Controllers;

[Area("Admin")]
public class IPDController : BaseController
{
    private readonly IDbLayer _dbLayer;

    public IPDController(IDbLayer dbLayer)
    {
        _dbLayer = dbLayer;
    }

    [HttpGet]
    public async Task<IActionResult> AddIPDAdmission()
    {
        var model = new IpdAdmission
        {
            PatientID = await GetNextPatientID() // Populate model.PatientID
        };

        // Populate Gender dropdown
        ViewBag.GenderList = new List<SelectListItem>
        {
            new SelectListItem { Text = "-- Select Gender --", Value = "" },
            new SelectListItem { Text = "Male", Value = "Male" },
            new SelectListItem { Text = "Female", Value = "Female" }
        };

        // ✅ Populate Doctor dropdown from SP
        ViewBag.DoctorList = await GetDoctorsDropdown();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddIPDAdmission(IpdAdmission model)
    {
        if (!ModelState.IsValid)
        {
            model.PatientID = await GetNextPatientID(); // Populate on validation fail

            // Reload dropdowns on validation fail
            ViewBag.GenderList = new List<SelectListItem>
            {
                new SelectListItem { Text = "-- Select Gender --", Value = "" },
                new SelectListItem { Text = "Male", Value = "Male" },
                new SelectListItem { Text = "Female", Value = "Female" }
            };
            ViewBag.DoctorList = await GetDoctorsDropdown();

            return View(model);
        }

        string patientId = await GetNextPatientID();

        SqlParameter[] parameters =
        {
            new SqlParameter("@Action", "INSERT"),
            new SqlParameter("@PatientID", patientId),
            new SqlParameter("@DoctorID", model.DoctorId),
            new SqlParameter("@BedCategoryId", model.BedCategoryId),
            new SqlParameter("@BedId", model.BedId),
            new SqlParameter("@AdmissionDateTime", model.AdmissionDateTime),
            new SqlParameter("@InitialDiagnosis", model.InitialDiagnosis ?? ""),
            new SqlParameter("@AdvanceAmount", model.AdvanceAmount),
            new SqlParameter("@Name", model.Name),
            new SqlParameter("@Age", model.Age),
            new SqlParameter("@Gender", model.Gender),
            new SqlParameter("@Number", model.Number),
            new SqlParameter("@Status", model.Status),
            new SqlParameter("@Address", DBNull.Value),
            new SqlParameter("@ProfilePath", DBNull.Value)
        };

        DataTable dt = await _dbLayer.ExecuteSPAsync("sp_IPDAdmission_CRUD", parameters);

        // Set PatientID back to model for textbox binding
        model.PatientID = dt.Rows.Count > 0 ? dt.Rows[0]["PatientID"].ToString() : patientId;

        // Reload dropdowns after save
        ViewBag.GenderList = new List<SelectListItem>
        {
            new SelectListItem { Text = "-- Select Gender --", Value = "" },
            new SelectListItem { Text = "Male", Value = "Male" },
            new SelectListItem { Text = "Female", Value = "Female" }
        };
        ViewBag.DoctorList = await GetDoctorsDropdown();

        ViewBag.Success = "IPD Admission Saved Successfully";
        return View(model);
    }

    // ✅ Get Next PatientID
    private async Task<string> GetNextPatientID()
    {
        string prefix = "UH";
        string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(PatientID,3,10) AS INT)),0) FROM IPDAdmissions";

        var result = await _dbLayer.ExecuteScalarAsync(query);
        int maxId = Convert.ToInt32(result);
        int nextId = maxId + 1;

        return prefix + nextId.ToString("D4"); // e.g., UH0001
    }


    private async Task<List<SelectListItem>> GetDoctorsDropdown()
    {
        var doctors = new List<SelectListItem>
    {
        new SelectListItem { Text = "-- Select Doctor --", Value = "" }
    };

        SqlParameter[] parameters =
        {
        new SqlParameter("@Action", "SelectDoctors"),
        new SqlParameter("@Search", DBNull.Value)
    };

        DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageDoctor", parameters);

        if (dt != null && dt.Rows.Count > 0)
        {
            foreach (DataRow row in dt.Rows)
            {
                doctors.Add(new SelectListItem
                {
                    Text = row["FullName"].ToString(),
                    Value = row["DoctorId"].ToString()
                });
            }
        }

        return doctors;
    }
   
}
