using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using HMSCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Globalization;

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
        [HttpGet]
        public async Task<IActionResult> AddPatient(string patientId = null)
        {
            var model = new AddPatientViewModel();

            if (!string.IsNullOrEmpty(patientId))
            { 
                    model.IsEdit = true;
                 

                // Load existing patient for edit
                var dtPatient = await _dbLayer.ExecuteSPAsync(
                    "sp_opdManagePatient",
                    new[] {
                new SqlParameter("@Action", "GetPatientById"),
                new SqlParameter("@PatientID", patientId)
                    }
                );

                if (dtPatient.Rows.Count > 0)
                {
                    var row = dtPatient.Rows[0];
                    model.PatientId = row["PatientID"].ToString();
                    model.PatientName = row["Name"].ToString();
                    model.Gender = row["Gender"].ToString();
                    model.DOB = DateTime.TryParse(row["DOB"]?.ToString(), out var dobVal) ? dobVal : (DateTime?)null; 
                    model.Age = row["Age"].ToString();
                    model.Address = row["Address1"].ToString();
                    model.ConsultFee = row["ConsultFee"] == DBNull.Value ? null : row["ConsultFee"].ToString(); 
                    model.ContactNo = row["ContactNo"].ToString();
                    model.SelectedDepartmentId = row["DepartmentId"] == DBNull.Value ? null : (int?)row["DepartmentId"];
                    model.SelectedDoctorId = row["DoctorId"] == DBNull.Value ? null : (int?)row["DoctorId"];
                    model.IsSaved = false;
                }
            }
            else
            {
                // 🔥 Generate new Patient ID
                var dtPid = await _dbLayer.ExecuteSPAsync(
                    "sp_opdManagePatient",
                    new[] { new SqlParameter("@Action", "GetNewPatientId") }
                );
                model.PatientId = dtPid.Rows[0]["NewPatientId"].ToString();
            }

            await LoadDropdowns(model);

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

            // Decide action: Insert or Update
            string action = model.IsEdit ? "UpdatePatient" : "InsertPatient";

            var parameters = new[]
            {
        new SqlParameter("@Action", action),
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

            // Show dynamic message based on action
            string messageAction = model.IsEdit ? "updated" : "saved";
            TempData["Message"] = $"Patient {model.PatientId} {messageAction} successfully!";
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

        //[HttpGet]
        //public async Task<IActionResult> PrintPreception(string pageName, string id)
        //{
        //    // 1. Configure HttpClientHandler
        //    var handler = new HttpClientHandler
        //    {
        //        // Bypass SSL certificate errors (only if necessary)
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,

        //        // Use server credentials to access protected WebForms pages
        //        UseDefaultCredentials = true
        //    };

        //    using var client = new HttpClient(handler);

        //    try
        //    {
        //        // 2. Get base URL from configuration
        //        var baseUrl = _configuration["WebFormBaseUrl"];

        //        // 3. Build the full WebForms page URL
        //        var url = $"{baseUrl}/{pageName}.aspx?PatientId={id}";

        //        // 4. Fetch the page as byte array (PDF)
        //        var pdfBytes = await client.GetByteArrayAsync(url);

        //        // 5. Return PDF to browser inline
        //        Response.Headers.Add("Content-Disposition", $"inline; filename=Patient_{id}.pdf");
        //        return File(pdfBytes, "application/pdf");
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        // If the WebForms page is unreachable or returns 403/404
        //        return StatusCode(500, $"Error fetching PDF: {ex.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        // Catch all other errors
        //        return StatusCode(500, $"Unexpected error: {ex.Message}");
        //    }
        //}

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
                .Select(r => new HMSCore.Areas.Admin.Models.Department
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
                    .Select(r => new HMSCore.Areas.Admin.Models.Doctor
                    {
                        DoctorId = r.Field<int>("DoctorId"),
                        FullName = r.Field<string>("DoctorName")
                    }).ToList();
            }
            else
            {
                model.Doctors = new List<HMSCore.Areas.Admin.Models.Doctor>();
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
        

            object fromDateValue = string.IsNullOrEmpty(fromDate)
                ? DBNull.Value
                : DateTime.ParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            object toDateValue = string.IsNullOrEmpty(toDate)
                ? DBNull.Value
                : DateTime.ParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(1);  


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


     

            [HttpGet]
            public async Task<IActionResult> CheckupHistory(string pid)
            {

                var model = new CheckupHistoryViewModel
                {
                    PatientId = pid
                };

                // 🔹 Patient Profile
                var dtPatient = await _dbLayer.ExecuteSPAsync(
                    "sp_opdCheckupHistory",
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
                    //model.BloodGroup = r["Blood_Group"].ToString();
                    model.DOB = DateTime.TryParse(r["dob"]?.ToString(), out var d) ? d : null;
                    model.Contact = r["ContactNo"].ToString();
                    model.Address = r["Address1"].ToString();
                    model.ProfilePath = r["ProfilePath"].ToString();
                }

                // 🔹 Checkups
                var dtCheckups = await _dbLayer.ExecuteSPAsync(
                    "sp_opdCheckupHistory",
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
                        "sp_opdCheckupHistory",
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

        // 🔥 DELETE Checkup
        [HttpPost]
        public async Task<IActionResult> DeleteCheckup(int checkupId)
        {
            try
            {
                await _dbLayer.ExecuteSPAsync(
                    "sp_opdManageCheckup",
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
        public async Task<IActionResult> AddNewCheckup(string pid)
        {
            if (string.IsNullOrWhiteSpace(pid))
            {
                TempData["Message"] = "Invalid patient.";
                TempData["MessageType"] = "error";
                return RedirectToAction("AddNewCheckup", "OPD");
            }

            var model = new AddCheckupViewModel
            {
                PatientId = pid
            };

            // -------- Doctors --------
            var dtDoctors = await _dbLayer.ExecuteSPAsync(
                "sp_opdManageCheckup",
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
                "sp_opdManageCheckup",
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
        public async Task<IActionResult> AddNewCheckup(AddCheckupViewModel model)
        {
            ModelState.Remove("MedicineName");
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Please fill all required fields.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(AddNewCheckup), new { pid = model.PatientId });
            }

            try
            {
                // -------- Insert Checkup --------
                var dt = await _dbLayer.ExecuteSPAsync(
                    "sp_opdManageCheckup",
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
                            "sp_opdManageCheckup",
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

                return RedirectToAction("CheckupHistory", "OPD", new { pid = model.PatientId });
            }
            catch
            {
                TempData["Message"] = "Something went wrong while saving checkup.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(AddNewCheckup), new { pid = model.PatientId });
            }
        }


        // ================= ADD CHECKUP (GET) =================
        [HttpGet]
        public async Task<IActionResult> EditCheckup(int checkupId)
        {
            var model = new AddCheckupViewModel();

            // ---------- Load Checkup ----------
            var dt = await _dbLayer.ExecuteSPAsync(
                "sp_opdManageCheckup",
                new[]
                {
            new SqlParameter("@Action","GetCheckupById"),
            new SqlParameter("@CheckupId", checkupId)
                });

            if (dt.Rows.Count == 0)
            {
                TempData["Message"] = "Checkup not found.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index");
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
                "sp_opdManageCheckup",
                new[] { new SqlParameter("@Action", "GetDoctors") });

            foreach (DataRow d in dtDoctors.Rows)
                model.Doctors.Add(new HMSCore.Areas.Admin.Models.Doctor
                {
                    DoctorId = Convert.ToInt32(d["DoctorId"]),
                    FullName = d["FullName"].ToString()
                });

            // ---------- Medicines ----------
            var dtMeds = await _dbLayer.ExecuteSPAsync(
                "sp_opdManageCheckup",
                new[] { new SqlParameter("@Action", "GetMedicines") });

            foreach (DataRow m in dtMeds.Rows)
                model.Medicines.Add(new MedicineVM
                {
                    MedicineId = Convert.ToInt32(m["MedicineId"]),
                    Name = m["MedicineName"].ToString()
                });

            // ---------- Prescriptions ----------
            var dtPres = await _dbLayer.ExecuteSPAsync(
                "sp_opdManageCheckup",
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
        public async Task<IActionResult> EditCheckup(AddCheckupViewModel model, int checkupId)
        {
            // UPDATE CHECKUP
            await _dbLayer.ExecuteSPAsync(
                "sp_opdManageCheckup",
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
                "sp_opdManageCheckup",
                new[]
                {
            new SqlParameter("@Action","DeletePrescriptions"),
            new SqlParameter("@CheckupId",checkupId)
                });

            // INSERT NEW PRESCRIPTIONS
            foreach (var p in model.Prescriptions)
            {
                await _dbLayer.ExecuteSPAsync(
                    "sp_opdManageCheckup",
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

            return RedirectToAction("CheckupHistory", "OPD", new { pid = model.PatientId });
        }

        [HttpGet]
        public async Task<IActionResult> PrintCheckupInvoice(string pageName, string checkupId)
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

     
        [HttpGet]
        public async Task<IActionResult> PatientsBill(
          string filterColumn = null,
          string keyword = null,
          string fromDate = null,
          string toDate = null,
          int pageSize = 20)
        {


            object fromDateValue = string.IsNullOrEmpty(fromDate)
                ? DBNull.Value
                : DateTime.ParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            object toDateValue = string.IsNullOrEmpty(toDate)
                ? DBNull.Value
                : DateTime.ParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(1);


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


        [HttpGet]
        public async Task<IActionResult> CreateBill(string patientId = null)
        {
            if (string.IsNullOrEmpty(patientId))
                return RedirectToAction("PatientsBill");

            var model = new CreateBillViewModel();

            // Generate next BillNo
            model.BillNo = await GetNextBillNoAsync("B00");
            model.PatientId = patientId;

            // Fetch patient details using SP
            var dt = await _dbLayer.ExecuteSPAsync(
                "sp_opdManagePatientBill",
                new[]
                {
            new SqlParameter("@Action", "SelectPatient"),
            new SqlParameter("@PatientID", patientId)
                }
            );

            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                model.PatientNo = row["PatientID"].ToString();
                model.Name = row["Name"].ToString();
                model.Age = row["Age"].ToString();
                model.Gender = row["Gender"].ToString();
                model.ConsultFee = string.IsNullOrEmpty(row["ConsultFee"].ToString())
                                    ? 0
                                    : Convert.ToDecimal(row["ConsultFee"]);
                model.BillDate = DateTime.Now;
            }

            // Initialize one empty treatment row
            model.Charges.Add(new TreatmentCharge());

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBill(CreateBillViewModel model, string actionType = "Insert")
        {
            ModelState.Remove("PatientId");
            ModelState.Remove("VID");
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Convert treatment charges to JSON
                var treatmentJson = Newtonsoft.Json.JsonConvert.SerializeObject(
                    model.Charges
                         .Where(c => !string.IsNullOrEmpty(c.Treatment))
                         .Select(c => new { c.Treatment, c.Charge })
                );

                await _dbLayer.ExecuteSPAsync(
                    "sp_opdManagePatientBill",
                    new SqlParameter[]
                    {
                new SqlParameter("@Action", actionType),
                new SqlParameter("@VID", model.PatientNo ?? ""),
                new SqlParameter("@BillNo", model.BillNo),
                new SqlParameter("@BillDate", model.BillDate),
                new SqlParameter("@PatientNo", model.PatientNo),
                new SqlParameter("@PatientID", model.PatientNo),
                new SqlParameter("@Name", model.Name),
                new SqlParameter("@Age", model.Age),
                new SqlParameter("@Gender", model.Gender),
                new SqlParameter("@ConsultFee", model.ConsultFee),
                new SqlParameter("@Discount", model.Discount),
                new SqlParameter("@PaidValue", model.PaidValue),
                new SqlParameter("@Balance", model.Balance),
                new SqlParameter("@TotalAmount", model.TotalAmount),
                new SqlParameter("@GrandTotal", model.GrandTotal),
                new SqlParameter("@Total", model.Total),
                new SqlParameter("@ExpectedDate", (object)model.ExpectedDate ?? DBNull.Value),
                new SqlParameter("@Advance", model.Advance),
                new SqlParameter("@TreatmentChargesJson", treatmentJson)
                    }
                );
                TempData["Message"] = $"Bill {model.BillNo} {(actionType == "Insert" ? "created" : "updated")} successfully";
                TempData["MessageType"] = "success"; 
                TempData["BillSaved"] = model.BillNo; 
                return View(model);
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }


        public async Task<string> GetNextBillNoAsync(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                prefix = "B00"; // default prefix

            // Prepare output parameter for SP
            var outputParam = new SqlParameter("@NextBillNo", SqlDbType.NVarChar, 50)
            {
                Direction = ParameterDirection.Output
            };

            // Call the SP through your DB layer
            await _dbLayer.ExecuteSPAsync(
                "sp_GenerateNextBillNo",
                new SqlParameter[]
                {
            new SqlParameter("@Prefix", prefix),
            outputParam
                }
            );

            // Return the value from the output parameter
            return outputParam.Value?.ToString() ?? prefix + "001";
        }

  
        [HttpGet]
        public async Task<IActionResult> PrintBilling(string pageName, string id)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler);

            var baseUrl = _configuration["WebFormBaseUrl"];
            var url = $"{baseUrl}/{pageName}.aspx?Id={id}";

            var pdfBytes = await client.GetByteArrayAsync(url);

            Response.Headers.Add("Content-Disposition", "inline; filename=Bill_{id}.pdf");
            return File(pdfBytes, "application/pdf");
        }

        [HttpGet]
        public async Task<IActionResult> EditBill(string billNo)
        {
            if (string.IsNullOrEmpty(billNo))
                return RedirectToAction("PatientsBill");

            var model = new CreateBillViewModel();

            try
            {
                // 🔹 1. Get patient billing
                var dtBill = await _dbLayer.ExecuteSPAsync(
                    "sp_opdManagePatientBill",
                    new[]
                    {
                new SqlParameter("@Action", "GetBillHeader"),
                new SqlParameter("@BillNo", billNo)
                    });

                if (dtBill.Rows.Count == 0)
                    return RedirectToAction("PatientsBill");

                var r = dtBill.Rows[0];
                model.BillNo = r["BillNo"].ToString();
                model.BillDate = Convert.ToDateTime(r["BillDate"]);
                model.PatientNo = r["PatientNo"].ToString();
                model.PatientId = r["PatientID"].ToString();
                model.Name = r["Name"].ToString();
                model.Age = r["Age"].ToString();
                model.Gender = r["Gender"].ToString();
                model.ConsultFee = Convert.ToDecimal(r["ConsultFee"]);
                model.Discount = Convert.ToDecimal(r["Discount"]);
                model.PaidValue = Convert.ToDecimal(r["PaidValue"]);
                model.TotalAmount = Convert.ToDecimal(r["TotalAmount"]);
                model.Total = Convert.ToDecimal(r["Total"]);
                model.GrandTotal = Convert.ToDecimal(r["GrandTotal"]);
                model.Balance = Convert.ToDecimal(r["Balance"]);

                // 🔹 2. Get treatment charges
                var dtCharges = await _dbLayer.ExecuteSPAsync(
                    "sp_opdManagePatientBill",
                    new[]
                    {
                new SqlParameter("@Action", "GetBillCharges"),
                new SqlParameter("@BillNo", billNo)
                    });

                foreach (DataRow row in dtCharges.Rows)
                {
                    model.Charges.Add(new TreatmentCharge
                    {
                        Treatment = row["Treatment"].ToString(),
                        Charge = Convert.ToDecimal(row["Charge"])
                    });
                }

                if (!model.Charges.Any())
                    model.Charges.Add(new TreatmentCharge());
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBill(CreateBillViewModel model)
        {
            ModelState.Remove("VID");
            ModelState.Remove("PatientId");
            if (!ModelState.IsValid)
                return View(model);

            var treatmentJson = JsonConvert.SerializeObject(
                model.Charges
                     .Where(x => !string.IsNullOrWhiteSpace(x.Treatment))
                     .Select(x => new { x.Treatment, x.Charge })
            );

            await _dbLayer.ExecuteSPAsync(
                "sp_opdManagePatientBill",
                new[]
                {
            new SqlParameter("@Action", "Update"),
            new SqlParameter("@VID", model.VID ?? ""),
            new SqlParameter("@BillNo", model.BillNo),
            new SqlParameter("@BillDate", model.BillDate),
            new SqlParameter("@PatientNo", model.PatientNo),
            new SqlParameter("@PatientID", model.PatientId),
            new SqlParameter("@Name", model.Name),
            new SqlParameter("@Age", model.Age),
            new SqlParameter("@Gender", model.Gender),
            new SqlParameter("@ConsultFee", model.ConsultFee),
            new SqlParameter("@Discount", model.Discount),
            new SqlParameter("@PaidValue", model.PaidValue),
            new SqlParameter("@Balance", model.Balance),
            new SqlParameter("@TotalAmount", model.TotalAmount),
            new SqlParameter("@GrandTotal", model.GrandTotal),
            new SqlParameter("@Total", model.Total),
            new SqlParameter("@TreatmentChargesJson", treatmentJson)
                });

            TempData["BillSaved"] = model.BillNo;
            TempData["Message"] = "Bill updated successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("EditBill", new { billNo = model.BillNo });
        }


        [HttpGet]
        public async Task<IActionResult> BillHistory(
            string filterColumn = null,
            string keyword = null,
            string fromDate = null,
            string toDate = null,
            int pageSize = 20)
        {
            object fromDateValue = string.IsNullOrEmpty(fromDate)
                ? DBNull.Value
                : DateTime.ParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            object toDateValue = string.IsNullOrEmpty(toDate)
                ? DBNull.Value
                : DateTime.ParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(1);

            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Action", "SelectBillHistory"),
        new SqlParameter("@FilterColumn", string.IsNullOrEmpty(filterColumn) ? DBNull.Value : (object)filterColumn),
        new SqlParameter("@Keyword", string.IsNullOrEmpty(keyword) ? DBNull.Value : (object)keyword),
        new SqlParameter("@FromDate", fromDateValue),
        new SqlParameter("@ToDate", toDateValue)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_OpdManageBillhistory", parameters);

            var bills = dt.AsEnumerable().Select(r => new PatientBillViewModel
            {
                BillId = r["BillId"] == DBNull.Value ? 0 : Convert.ToInt32(r["BillId"]),
                BillNo = r["BillNo"]?.ToString(),
                BillDate = r["BillDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["BillDate"]),
                PatientNo = r["PatientNo"]?.ToString(),
                PatientID = r["PatientID"]?.ToString(),
                Name = r["Name"]?.ToString(),
                Age = r["Age"]?.ToString(),
                Gender = r["Gender"]?.ToString(),
                ConsultFee = r["ConsultFee"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["ConsultFee"]),
                GrandTotal = r["GrandTotal"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["GrandTotal"])
            }).ToList();

            var vm = new PatientBillViewModel
            {
                PageSize = pageSize,
                FilterColumn = filterColumn,
                Keyword = keyword,
                FromDate = string.IsNullOrEmpty(fromDate) ? (DateTime?)null : DateTime.Parse(fromDate),
                ToDate = string.IsNullOrEmpty(toDate) ? (DateTime?)null : DateTime.Parse(toDate),
                Bills = bills
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBill(string billNo)
        {
            await _dbLayer.ExecuteSPAsync(
                "sp_opdManagePatientBill",
                new[]
                {
            new SqlParameter("@Action","DeleteBill"),
            new SqlParameter("@BillNo",billNo)
                }
            );

            TempData["Message"] = $"Bill {billNo} deleted successfully"; 
            TempData["MessageType"] = "success";
            return RedirectToAction("BillHistory");
        }
        [HttpPost]
        public async Task<IActionResult> DeleteSelectedBill(string selectedIds)
        {
            if (!string.IsNullOrWhiteSpace(selectedIds))
            {
                var billNos = selectedIds.Split(',');

                foreach (var billNo in billNos)
                {
                    await _dbLayer.ExecuteSPAsync(
                        "sp_opdManagePatientBill",
                        new[]
                        {
                    new SqlParameter("@Action", "DeleteBill"),
                    new SqlParameter("@BillNo", billNo)
                        }
                    );
                }

                TempData["Message"] = "Selected bills deleted successfully";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "No bills selected";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("BillHistory");
        }


        [HttpGet]
        public async Task<IActionResult> TodayPatientsDetails(
             string filterColumn = null,
             string keyword = null,
             string fromDate = null,
             string toDate = null,
             int pageSize = 20)
        {


            object fromDateValue = string.IsNullOrEmpty(fromDate)
                ? DBNull.Value
                : DateTime.ParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            object toDateValue = string.IsNullOrEmpty(toDate)
                ? DBNull.Value
                : DateTime.ParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(1);


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
            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_OpdManageTodayPatients", parameters);

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
        public async Task<IActionResult> DeleteTodayPatients(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_OpdManageTodayPatients", new[]
            {
        new SqlParameter("@Action", "DeletePatient"),
        new SqlParameter("@PatientId", id)
    });

            TempData["Message"] = "Patient deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("PatientsDetails");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTodayPatientSelected(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_OpdManageTodayPatients", new[]
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
