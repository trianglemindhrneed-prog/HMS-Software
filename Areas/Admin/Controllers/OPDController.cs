using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HMSCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OPDController : BaseController
    {
        private readonly IDbLayer _dbLayer;
        private readonly IConfiguration _configuration;
        public OPDController(IDbLayer dbLayer, IConfiguration configuration)
        {
            _dbLayer = dbLayer;
            _configuration = configuration;
        } 
        public async Task<IActionResult> AddPatient()
        {
            var model = new AddPatientViewModel();

            // 🔥 Generate Patient ID
            var dtPid = await _dbLayer.ExecuteSPAsync(
                "sp_opdManagePatient",
                new[] { new SqlParameter("@Action", "GetNewPatientId") }
            );

            model.PatientId = dtPid.Rows[0]["NewPatientId"].ToString();

            // Load Departments
            var dtDept = await _dbLayer.ExecuteSPAsync(
                "sp_opdManagePatient",
                new[] { new SqlParameter("@Action", "GetDepartments") }
            );

            model.Departments = dtDept.AsEnumerable()
                .Select(r => new Department
                {
                    DepartmentId = r.Field<int>("DepartmentId"),
                    DepartmentName = r.Field<string>("DepartmentName")
                }).ToList();

            model.DepartmentList =
                new SelectList(model.Departments, "DepartmentId", "DepartmentName");

            // Empty doctors initially
            model.Doctors = new List<Doctor>();
            model.DoctorList =
                new SelectList(model.Doctors, "DoctorId", "DoctorName");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddPatient(AddPatientViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model);
                return View(model);
            }

            var parameters = new[]
            {
        new SqlParameter("@Action", "InsertPatient"),
        new SqlParameter("@PatientID", model.PatientId),
        new SqlParameter("@Name", model.PatientName ?? (object)DBNull.Value),
        new SqlParameter("@Gender", model.Gender ?? (object)DBNull.Value),
        new SqlParameter("@DOB", model.DOB ?? (object)DBNull.Value),
        new SqlParameter("@Age", model.Age ?? (object)DBNull.Value),
        new SqlParameter("@Address1", model.Address ?? (object)DBNull.Value),
        new SqlParameter("@ConsultFee", model.ConsultFee ?? (object)DBNull.Value),
        new SqlParameter("@ContactNo", model.ContactNo ?? (object)DBNull.Value),
        new SqlParameter("@DepartmentId", model.SelectedDepartmentId ?? (object)DBNull.Value),
        new SqlParameter("@DoctorId", model.SelectedDoctorId ?? (object)DBNull.Value)
    };

            await _dbLayer.ExecuteSPAsync("sp_opdManagePatient", parameters);
             
            model.IsSaved = true;
            TempData["Message"] = $"Patient {model.PatientId} added successfully!";
            TempData["MessageType"] = "success";
            await LoadDropdowns(model); 

            return View(model);  
        } 

        [HttpGet]
        public async Task<IActionResult> PrintPreception(string pageName, string id) 
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler);

            var baseUrl = _configuration["WebFormBaseUrl"];
            var url = $"{baseUrl}/{pageName}.aspx?PatientId={id}";

            var pdfBytes = await client.GetByteArrayAsync(url);

            Response.Headers.Add("Content-Disposition", "inline; filename=Patient_{id}.pdf");
            return File(pdfBytes, "application/pdf");
        }

        private async Task LoadDropdowns(AddPatientViewModel model)
        {
            // 🔥 Ensure PatientId exists
            if (string.IsNullOrEmpty(model.PatientId))
            {
                var dtPid = await _dbLayer.ExecuteSPAsync(
                    "sp_opdManagePatient",
                    new[] { new SqlParameter("@Action", "GetNewPatientId") }
                );
                model.PatientId = dtPid.Rows[0]["NewPatientId"].ToString();
            }

            // Load Departments
            var dtDept = await _dbLayer.ExecuteSPAsync(
                "sp_opdManagePatient",
                new[] { new SqlParameter("@Action", "GetDepartments") }
            );

            model.Departments = dtDept.AsEnumerable()
                .Select(r => new Department
                {
                    DepartmentId = r.Field<int>("DepartmentId"),
                    DepartmentName = r.Field<string>("DepartmentName")
                }).ToList();

            model.DepartmentList = new SelectList(
                model.Departments,
                "DepartmentId",
                "DepartmentName",
                model.SelectedDepartmentId
            );

            // Load Doctors if department selected
            if (model.SelectedDepartmentId.HasValue)
            {
                var dtDoc = await _dbLayer.ExecuteSPAsync(
                    "sp_opdManagePatient",
                    new[]
                    {
                new SqlParameter("@Action", "GetDoctors"),
                new SqlParameter("@DepartmentId", model.SelectedDepartmentId.Value)
                    });

                model.Doctors = dtDoc.AsEnumerable()
                    .Select(r => new Doctor
                    {
                        DoctorId = r.Field<int>("DoctorId"),
                        FullName = r.Field<string>("DoctorName")
                    }).ToList();
            }
            else
            {
                model.Doctors = new List<Doctor>();
            }

            model.DoctorList = new SelectList(
      model.Doctors,
      "DoctorId",
      "FullName",  
      model.SelectedDoctorId
  );

        }

        [HttpGet]
        public async Task<JsonResult> GetDoctorsByDepartment(int departmentId)
        {
            var dtDoc = await _dbLayer.ExecuteSPAsync(
                "sp_opdManagePatient",
                new[]
                {
            new SqlParameter("@Action", "GetDoctors"),
            new SqlParameter("@DepartmentId", departmentId)
                });

            var doctors = dtDoc.AsEnumerable()
                .Select(r => new
                {
                    DoctorId = r.Field<int>("DoctorId"),
                    DoctorName = r.Field<string>("DoctorName")
                }).ToList();

            return Json(doctors);
        }


        [HttpGet]
        public async Task<IActionResult> PatientsDetails(
          string filterColumn = null,
          string keyword = null,
          string fromDate = null,
          string toDate = null,
          int pageSize = 20)
        {
            // Parse date filters and handle full day for ToDate
            object fromDateValue = string.IsNullOrEmpty(fromDate)
                ? DBNull.Value
                : DateTime.Parse(fromDate);

            object toDateValue = string.IsNullOrEmpty(toDate)
                ? DBNull.Value
                : DateTime.Parse(toDate).AddDays(1).AddTicks(-1); // Include full day

            // Prepare SQL parameters for the stored procedure
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Action", "SelectPatients"),
        new SqlParameter("@FilterColumn", string.IsNullOrEmpty(filterColumn) ? DBNull.Value : (object)filterColumn),
        new SqlParameter("@Keyword", string.IsNullOrEmpty(keyword) ? DBNull.Value : (object)keyword),
        new SqlParameter("@FromDate", fromDateValue),
        new SqlParameter("@ToDate", toDateValue)
            };

            // Execute stored procedure
            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_OpdManagePatients", parameters);

            // Map DataTable to ViewModel with DBNull-safe conversions
            var patients = dt.AsEnumerable().Select(r => new PatientsDetailsViewModel
            {
                Id = r["Id"] == DBNull.Value ? 0 : Convert.ToInt32(r["Id"]),
                PatientId = r["PatientId"] == DBNull.Value ? null : r["PatientId"].ToString(),
                PatientName = r["PatientName"] == DBNull.Value ? null : r["PatientName"].ToString(),
                Age = r["Age"] == DBNull.Value ? null : r["Age"].ToString(),
                ContactNo = r["ContactNo"] == DBNull.Value ? null : r["ContactNo"].ToString(),
                Address1 = r["Address1"] == DBNull.Value ? null : r["Address1"].ToString(),
                ConsultFee = r["ConsultFee"] == DBNull.Value ? null : r["ConsultFee"].ToString(),
                DepartmentName = r["DepartmentName"] == DBNull.Value ? null : r["DepartmentName"].ToString(),
                DoctorName = r["DoctorName"] == DBNull.Value ? null : r["DoctorName"].ToString(),
                DoctorNumber = r["DoctorNumber"] == DBNull.Value ? null : r["DoctorNumber"].ToString(),
                CreatedDate = r["CreatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CreatedDate"])
            }).ToList();

            // Prepare ViewModel for the view
            var vm = new PatientsDetailsViewModel
            {
                PageSize = pageSize,
                FilterColumn = filterColumn,
                Keyword = keyword,
                FromDate = string.IsNullOrEmpty(fromDate) ? (DateTime?)null : DateTime.Parse(fromDate),
                ToDate = string.IsNullOrEmpty(toDate) ? (DateTime?)null : DateTime.Parse(toDate),
                Patients = patients
            };

            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> DeletePatients(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_OpdManagePatients", new[]
            {
        new SqlParameter("@Action", "DeletePatient"),
        new SqlParameter("@PatientId", id)
    });

            TempData["Message"] = "Patient deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("PatientsDetails");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelected(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_OpdManagePatients", new[]
                    {
                new SqlParameter("@Action", "DeletePatient"),
                new SqlParameter("@PatientId", id)
            });
                }

                TempData["Message"] = "Selected patients deleted successfully";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "No patients selected";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("PatientsDetails");
        }







    }
}
