using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HMSCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MasterController : BaseController
    {
        private readonly IDbLayer _dbLayer;

        public MasterController(IDbLayer dbLayer)
        {
            _dbLayer = dbLayer;
        }


        [HttpGet]
        public async Task<IActionResult> DepartmentList(string search = null, int? status = null)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "Select"),
                new SqlParameter("@FilterVal", string.IsNullOrEmpty(search) ? DBNull.Value : search),
                new SqlParameter("@Status", status.HasValue ? status.Value : (object)DBNull.Value)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageDepartment", parameters);

            var departments = dt.AsEnumerable().Select(r => new Department
            {
                DepartmentId = Convert.ToInt32(r["DepartmentId"]),
                DepartmentName = r["DepartmentName"].ToString(),
                IsActive = Convert.ToBoolean(r["IsActive"])
            }).ToList();

            ViewData["Search"] = search;
            ViewData["Status"] = status;

            return View(departments);
        }


        [HttpGet]
        public async Task<IActionResult> AddDepartment(int? id)
        {
            if (id == null)
                return View(new Department { IsActive = true });

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "SelectBYId"),
                new SqlParameter("@DepartmentId", id)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageDepartment", parameters);

            if (dt.Rows.Count == 0) return NotFound();

            var dept = new Department
            {
                DepartmentId = Convert.ToInt32(dt.Rows[0]["DepartmentId"]),
                DepartmentName = dt.Rows[0]["DepartmentName"].ToString(),
                IsActive = Convert.ToBoolean(dt.Rows[0]["IsActive"])
            };

            return View(dept);
        }

        [HttpPost]
        public async Task<IActionResult> AddDepartment(Department model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Please fill all required fields.";
                TempData["MessageType"] = "error";
                return View(model);
            }

            try
            {
                string action = model.DepartmentId > 0 ? "Update" : "Insert";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@Action", action),
            new SqlParameter("@DepartmentId", model.DepartmentId),
            new SqlParameter("@DepartmentName", model.DepartmentName),
            new SqlParameter("@IsActive", model.IsActive)
                };

                await _dbLayer.ExecuteSPAsync("sp_ManageDepartment", parameters);

                TempData["Message"] = action == "Insert"
                    ? "Department added successfully!"
                    : "Department updated successfully!";

                TempData["MessageType"] = "success";
            }
            catch (SqlException ex)
            {
                // Handle RAISERROR from SP
                TempData["Message"] = ex.Message;
                TempData["MessageType"] = "error";

                return View(model); // Return to the same view with input data
            }
            catch (Exception)
            {
                TempData["Message"] = "Something went wrong. Please try again.";
                TempData["MessageType"] = "error";
                return View(model);
            }

            return RedirectToAction("DepartmentList");
        }


        [HttpPost]
        public async Task<IActionResult> ToggleDepartmentStatus(int id)
        {
            try
            {
                await _dbLayer.ExecuteSPAsync("sp_ManageDepartment", new[]
                {
            new SqlParameter("@Action", "ToggleStatus"),
            new SqlParameter("@DepartmentId", id)
        });

                TempData["Message"] = "Department status updated successfully!";
                TempData["MessageType"] = "success";
            }
            catch
            {
                TempData["Message"] = "Unable to update department status.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("DepartmentList");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            try
            {
                await _dbLayer.ExecuteSPAsync("sp_ManageDepartment", new[]
                {
            new SqlParameter("@Action", "Delete"),
            new SqlParameter("@DepartmentId", id)
        });

                TempData["Message"] = "Department deleted successfully!";
                TempData["MessageType"] = "success";
            }
            catch
            {
                TempData["Message"] = "Unable to delete department.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("DepartmentList");
        }


        [HttpPost]
        public async Task<IActionResult> DeleteSelectedDepartments(int[] selectedIds)
        {
            try
            {
                if (selectedIds == null || selectedIds.Length == 0)
                {
                    TempData["Message"] = "Please select at least one department.";
                    TempData["MessageType"] = "warning";
                    return RedirectToAction("DepartmentList");
                }

                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManageDepartment", new SqlParameter[]
                    {
                new SqlParameter("@Action", "Delete"),
                new SqlParameter("@DepartmentId", id)
                    });
                }

                TempData["Message"] = $"{selectedIds.Length} department(s) deleted successfully!";
                TempData["MessageType"] = "success";
            }
            catch (Exception)
            {
                TempData["Message"] = "Unable to delete selected departments.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("DepartmentList");
        }

 

            // ================== Doctors List ==================
            [HttpGet]
            public async Task<IActionResult> DoctorsList(string search = null)
            {
                SqlParameter[] parameters =
                {
                new SqlParameter("@Action","SelectDoctors"),
                new SqlParameter("@Search", string.IsNullOrEmpty(search) ? DBNull.Value : search)
            };

                DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageDoctor", parameters);

                var doctors = dt.AsEnumerable().Select(r => new Doctor
                {
                    DoctorId = Convert.ToInt32(r["DoctorId"]),
                    FullName = r["FullName"].ToString(),
                    DepartmentName = r["DepartmentName"].ToString(),
                    DEmail = r["Email"].ToString(),
                    MobileNu = r["MobileNu"].ToString(),
                    Address = r["Address"].ToString(),
                    IsActive = Convert.ToBoolean(r["IsActive"])
                }).ToList();

                ViewData["Search"] = search;
                return View(doctors);
            }

            // ================== Single Doctor Action (Toggle / Delete) ==================
            [HttpPost]
            public async Task<IActionResult> ManageDoctor(int id, string actionType)
            {
                try
                {
                    string action = actionType switch
                    {
                        "Delete" => "DeleteDoctor",
                        "ToggleStatus" => "ToggleDoctorStatus",
                        _ => null
                    };

                    if (action == null)
                    {
                        TempData["Message"] = "Invalid action requested.";
                        TempData["MessageType"] = "error";
                        return RedirectToAction("DoctorsList");
                    }

                    await _dbLayer.ExecuteSPAsync("sp_ManageDoctor", new[]
                    {
                    new SqlParameter("@Action", action),
                    new SqlParameter("@DoctorId", id)
                });

                    TempData["Message"] = action switch
                    {
                        "DeleteDoctor" => "Doctor deleted successfully!",
                        "ToggleDoctorStatus" => "Doctor status updated successfully!",
                        _ => "Operation completed successfully!"
                    };
                    TempData["MessageType"] = "success";
                }
                catch (Exception)
                {
                    TempData["Message"] = "Something went wrong while processing doctor.";
                    TempData["MessageType"] = "error";
                }

                return RedirectToAction("DoctorsList");
            }

            // ================== Bulk Delete Doctors ==================
            [HttpPost]
            public async Task<IActionResult> DeleteSelectedDoctors(int[] selectedIds)
            {
                try
                {
                    if (selectedIds == null || selectedIds.Length == 0)
                    {
                        TempData["Message"] = "Please select at least one doctor.";
                        TempData["MessageType"] = "warning";
                        return RedirectToAction("DoctorsList");
                    }

                    foreach (var id in selectedIds)
                    {
                        await _dbLayer.ExecuteSPAsync("sp_ManageDoctor", new[]
                        {
                        new SqlParameter("@Action", "DeleteDoctor"),
                        new SqlParameter("@DoctorId", id)
                    });
                    }

                    TempData["Message"] = $"{selectedIds.Length} doctor(s) deleted successfully!";
                    TempData["MessageType"] = "success";
                }
                catch (Exception)
                {
                    TempData["Message"] = "Unable to delete selected doctors.";
                    TempData["MessageType"] = "error";
                }

                return RedirectToAction("DoctorsList");
            }
      

         [HttpPost]
        public async Task<IActionResult> ToggleDoctorStatus(int id)
        {
            try
            {
                // SP call to toggle status
                await _dbLayer.ExecuteSPAsync("sp_ManageDoctor", new[]
                {
            new SqlParameter("@Action", "ToggleDoctorStatus"),
            new SqlParameter("@DoctorId", id)
        });

                TempData["Message"] = "Doctor status updated successfully!";
                TempData["MessageType"] = "success";
            }
            catch (Exception)
            {
                TempData["Message"] = "Unable to update doctor status.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("DoctorsList");
        }

        [HttpGet]
        public async Task<IActionResult> AddDoctor(int? id)
        {
            Doctor model = new Doctor();

            // Load Departments for dropdown
            DataTable dtDept = await _dbLayer.ExecuteSPAsync(
                "sp_ManageDoctor",
                new SqlParameter[] { new SqlParameter("@Action", "SelectDepartments") }
            );

            model.Departments = dtDept.AsEnumerable()
                .Select(r => new Department
                {
                    DepartmentId = Convert.ToInt32(r["DepartmentId"]),
                    DepartmentName = r["DepartmentName"].ToString()
                }).ToList();

            if (id == null)
            {
                model.IsActive = true; // default active for new doctor
                return View(model);
            }

            // Edit Doctor
            DataTable dt = await _dbLayer.ExecuteSPAsync(
                "sp_ManageDoctor",
                new SqlParameter[]
                {
            new SqlParameter("@Action", "SelectDoctorById"),
            new SqlParameter("@DoctorId", id)
                }
            );

            if (dt.Rows.Count > 0)
            {
                var r = dt.Rows[0];
                model.DoctorId = Convert.ToInt32(r["DoctorId"]);
                model.DepartmentId = Convert.ToInt32(r["DepartmentId"]);
                model.FullName = r["FullName"].ToString();
                model.DEmail = r["Email"].ToString();
                model.Password = r["Password"].ToString(); // from UserAccount
                model.MobileNu = r["MobileNu"].ToString();
                model.ProfileImagePath = r["ProfileImagePath"].ToString();
                model.Address = r["Address"].ToString();
                model.IsActive = Convert.ToBoolean(r["IsActive"]);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddDoctor(Doctor model, IFormFile ProfileImage)
        {
            // Load Departments for dropdown
            DataTable dtDept = await _dbLayer.ExecuteSPAsync(
                "sp_ManageDoctor",
                new SqlParameter[] { new SqlParameter("@Action", "SelectDepartments") }
            );

            model.Departments = dtDept.AsEnumerable()
                .Select(r => new Department
                {
                    DepartmentId = Convert.ToInt32(r["DepartmentId"]),
                    DepartmentName = r["DepartmentName"].ToString()
                }).ToList();

            // Remove non-required ModelState fields
            ModelState.Remove("DepartmentName");
            ModelState.Remove("Departments");
            ModelState.Remove("Address");
            ModelState.Remove("ProfileImagePath");
            ModelState.Remove("ProfileImage");

            // Handle password: fetch existing if blank on update
            if (string.IsNullOrWhiteSpace(model.Password) && model.DoctorId != 0)
            {
                string existingPassword = await GetExistingDoctorPassword(model.DoctorId);
                if (!string.IsNullOrWhiteSpace(existingPassword))
                {
                    model.Password = existingPassword;
                    ModelState.Remove("Password");
                }
            }

            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Please fill all required doctor details.";
                TempData["MessageType"] = "error";
                return View(model);
            }

            // Handle Profile Image Upload
            string profilePath = model.ProfileImagePath;
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/doctors");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ProfileImage.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(fileStream);
                }

                profilePath = "/uploads/doctors/" + uniqueFileName;
            }

            try
            {
                string action = model.DoctorId == 0 ? "InsertDoctor" : "UpdateDoctor";

                SqlParameter[] parameters =
                {
            new SqlParameter("@Action", action),
            new SqlParameter("@DoctorId", model.DoctorId),
            new SqlParameter("@DepartmentId", model.DepartmentId),
            new SqlParameter("@FullName", model.FullName),
            new SqlParameter("@Email", model.DEmail),
            new SqlParameter("@MobileNu", model.MobileNu),
            new SqlParameter("@Address", model.Address),
            new SqlParameter("@Password", model.Password),
            new SqlParameter("@IsActive", model.IsActive),
            new SqlParameter("@ProfileImagePath", profilePath)
        };

                await _dbLayer.ExecuteSPAsync("sp_ManageDoctor", parameters);

                TempData["Message"] = action == "InsertDoctor"
                    ? "Doctor added successfully!"
                    : "Doctor updated successfully!";
                TempData["MessageType"] = "success";
            }
            catch (SqlException ex)
            {
                // Catch RAISERROR from SP for duplicates
                TempData["Message"] = ex.Message;
                TempData["MessageType"] = "error";
                return View(model);
            }
            catch (Exception)
            {
                TempData["Message"] = "Something went wrong while saving doctor.";
                TempData["MessageType"] = "error";
                return View(model);
            }

            return RedirectToAction("DoctorsList");
        }


        // ============================
        // Helper method to get existing password
        // ============================
        private async Task<string> GetExistingDoctorPassword(int doctorId)
        {
            if (doctorId <= 0) return string.Empty;

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageDoctor", new SqlParameter[]
            {
        new SqlParameter("@Action", "SelectDoctorById"),
        new SqlParameter("@DoctorId", doctorId)
            });

            if (dt.Rows.Count == 0) return string.Empty;

            return dt.Rows[0]["Password"]?.ToString() ?? string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> NurseList(string search = null, int? status = null)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Action", "Select"),
        new SqlParameter("@Search", string.IsNullOrEmpty(search) ? DBNull.Value : search),
        new SqlParameter("@Status", status.HasValue ? (object)status.Value : DBNull.Value)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageNurse", parameters);

            var nurses = dt.AsEnumerable().Select(r => new Nurse
            {
                NurseId = Convert.ToInt32(r["NurseId"]),
                FullName = r["FullName"].ToString(),
                Email = r["Email"].ToString(),
                MobileNu = r["MobileNu"].ToString(),
                IsActive = Convert.ToBoolean(r["IsActive"])
            }).ToList();

            ViewData["Search"] = search;
            ViewData["Status"] = status;

            return View(nurses);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleNurseStatus(int id)
        {
            try
            {
                await _dbLayer.ExecuteSPAsync("sp_ManageNurse", new[]
                {
            new SqlParameter("@Action", "ToggleStatus"),
            new SqlParameter("@NurseId", id)
        });

                TempData["Message"] = "Nurse status updated successfully!";
                TempData["MessageType"] = "success";
            }
            catch (Exception)
            {
                TempData["Message"] = "Unable to update nurse status.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("NurseList");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNurse(int id)
        {
            try
            {
                await _dbLayer.ExecuteSPAsync("sp_ManageNurse", new[]
                {
            new SqlParameter("@Action", "Delete"),
            new SqlParameter("@NurseId", id)
        });

                TempData["Message"] = "Nurse deleted successfully!";
                TempData["MessageType"] = "success";
            }
            catch (Exception)
            {
                TempData["Message"] = "Unable to delete nurse.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("NurseList");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedNurses(int[] selectedIds)
        {
            try
            {
                if (selectedIds == null || selectedIds.Length == 0)
                {
                    TempData["Message"] = "Please select at least one nurse.";
                    TempData["MessageType"] = "warning";
                    return RedirectToAction("NurseList");
                }

                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManageNurse", new[]
                    {
                new SqlParameter("@Action", "Delete"),
                new SqlParameter("@NurseId", id)
            });
                }

                TempData["Message"] = $"{selectedIds.Length} nurse(s) deleted successfully!";
                TempData["MessageType"] = "success";
            }
            catch (Exception)
            {
                TempData["Message"] = "Unable to delete selected nurses.";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("NurseList");
        }



        [HttpGet]
        public async Task<IActionResult> AddNurse(int? id)
        {
            Nurse model = new Nurse { IsActive = true };

            if (id == null)
                return View(model);

            // Fetch single nurse including password
            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageNurse", new SqlParameter[]
            {
        new SqlParameter("@Action", "SelectById"),
        new SqlParameter("@NurseId", id)
            });

            if (dt.Rows.Count == 0) return NotFound();

            var r = dt.Rows[0];
            model.NurseId = Convert.ToInt32(r["NurseId"]);
            model.FullName = r["FullName"].ToString();
            model.FullName = r["FullName"].ToString();
            model.Email = r["Email"].ToString();
            model.ProfileImagePath = r["ProfileImagePath"].ToString();
            model.MobileNu = r["MobileNu"].ToString();
            model.IsActive = Convert.ToBoolean(r["IsActive"]);
            model.Password = r.Table.Columns.Contains("Password") ? r["Password"].ToString() : null;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddNurse(Nurse model)
        {
            // ----------------------------
            // Handle image upload
            // ----------------------------
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/nurses");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImage.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(stream);
                }

                model.ProfileImagePath = "/uploads/nurses/" + fileName;
            }

            // ----------------------------
            // Remove unnecessary fields for validation
            // ----------------------------
            ModelState.Remove("Departments");
            ModelState.Remove("ProfileImage");
            ModelState.Remove("Address");

            // ----------------------------
            // Handle password
            // ----------------------------
            if (string.IsNullOrWhiteSpace(model.Password) && model.NurseId != 0)
            {
                var existingPassword = await GetExistingNursePassword(model.NurseId);
                if (!string.IsNullOrWhiteSpace(existingPassword))
                {
                    model.Password = existingPassword;
                    ModelState.Remove("Password");
                }
            }

            // ----------------------------
            // Validate
            // ----------------------------
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Please fill all required nurse details.";
                TempData["MessageType"] = "error";
                return View(model);
            }

            // ----------------------------
            // Save via SP
            // ----------------------------
            try
            {
                string action = model.NurseId == 0 ? "Insert" : "Update";

                SqlParameter[] parameters =
                {
            new SqlParameter("@Action", action),
            new SqlParameter("@NurseId", model.NurseId),
            new SqlParameter("@FullName", model.FullName),
            new SqlParameter("@Email", model.Email),
            new SqlParameter("@MobileNu", model.MobileNu),
            new SqlParameter("@IsActive", model.IsActive),
            new SqlParameter("@Password", model.Password),
            new SqlParameter("@ProfileImagePath", (object)model.ProfileImagePath ?? DBNull.Value)
        };

                await _dbLayer.ExecuteSPAsync("sp_ManageNurse", parameters);

                TempData["Message"] = action == "Insert" ? "Nurse added successfully!" : "Nurse updated successfully!";
                TempData["MessageType"] = "success";
            }
            catch (SqlException ex)
            {
                // Handle duplicate error from SP
                TempData["Message"] = ex.Message;
                TempData["MessageType"] = "error";
                return View(model);
            }
            catch (Exception)
            {
                TempData["Message"] = "Something went wrong while saving nurse.";
                TempData["MessageType"] = "error";
                return View(model);
            }

            return RedirectToAction("NurseList");
        }

        // ============================
        // Helper: Get existing password
        // ============================
        private async Task<string> GetExistingNursePassword(int nurseId)
        {
            if (nurseId <= 0) return string.Empty;

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageNurse", new SqlParameter[]
            {
        new SqlParameter("@Action", "SelectById"),
        new SqlParameter("@NurseId", nurseId)
            });

            if (dt.Rows.Count == 0) return string.Empty;

            return dt.Rows[0]["Password"]?.ToString() ?? string.Empty;
        }



    }
}
