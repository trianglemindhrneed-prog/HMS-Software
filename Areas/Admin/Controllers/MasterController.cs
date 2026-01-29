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
                return View(new Department { IsActive = true }); // new

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "Select"),
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
            if (!ModelState.IsValid) return View(model);

            string action = model.DepartmentId > 0 ? "Update" : "Insert";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", action),
                new SqlParameter("@DepartmentId", model.DepartmentId),
                new SqlParameter("@DepartmentName", model.DepartmentName),
                new SqlParameter("@IsActive", model.IsActive)
            };

            await _dbLayer.ExecuteSPAsync("sp_ManageDepartment", parameters);

            return RedirectToAction("DepartmentList");
        }
        [HttpPost]
        public async Task<IActionResult> ManageDepartment(int id, string actionType)
        {
            string spAction = actionType switch
            {
                "Delete" => "Delete",
                "ToggleStatus" => "ToggleStatus",
                _ => null
            };

            if (spAction == null) return BadRequest();

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", spAction),
                new SqlParameter("@DepartmentId", id)
            };

            await _dbLayer.ExecuteSPAsync("sp_ManageDepartment", parameters);

            return RedirectToAction("DepartmentList");
        }


        [HttpPost]
        public async Task<IActionResult> DeleteSelectedDepartments(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManageDepartment", new SqlParameter[]
                    {
                new SqlParameter("@Action", "Delete"),
                new SqlParameter("@DepartmentId", id)
                    });
                }
            }
            return RedirectToAction("DepartmentList");
        }


        [HttpGet]
        public async Task<IActionResult> DoctorsList(string search = null)
        {
            SqlParameter[] parameters =
            {
        new SqlParameter("@Action","SelectDoctors"),
        new SqlParameter("@Search",
            string.IsNullOrEmpty(search) ? DBNull.Value : search)
    };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageDoctor", parameters);

            var doctors = dt.AsEnumerable().Select(r => new Doctor
            {
                DoctorId = Convert.ToInt32(r["DoctorId"]),
                FullName = r["FullName"].ToString(),
                DepartmentName = r["DepartmentName"].ToString(),
                Email = r["Email"].ToString(),
                MobileNu = r["MobileNu"].ToString(),
                Address = r["Address"].ToString(),
                IsActive = Convert.ToBoolean(r["IsActive"])
            }).ToList();

            ViewData["Search"] = search;
            return View(doctors);
        }
        [HttpPost]
        public async Task<IActionResult> ManageDoctor(int id, string actionType)
        {
            string action = actionType switch
            {
                "Delete" => "DeleteDoctor",
                "ToggleStatus" => "ToggleDoctorStatus",
                _ => null
            };

            if (action == null) return BadRequest();

            await _dbLayer.ExecuteSPAsync("sp_ManageDoctor", new[]
            {
        new SqlParameter("@Action", action),
        new SqlParameter("@DoctorId", id)
    });

            return RedirectToAction("DoctorsList");
        }
        [HttpPost]
        public async Task<IActionResult> DeleteSelectedDoctors(int[] selectedIds)
        {
            if (selectedIds != null)
            {
                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManageDoctor", new[]
                    {
                new SqlParameter("@Action","DeleteDoctor"),
                new SqlParameter("@DoctorId", id)
                 });
                }
            }
            return RedirectToAction("DoctorsList");
        }
        [HttpGet]
        public async Task<IActionResult> AddDoctor(int? id)
        {
            Doctor model = new Doctor();

            // Departments dropdown from SP
            DataTable dtDept = await _dbLayer.ExecuteSPAsync(
                "sp_ManageDoctor",
                new SqlParameter[]
                {
            new SqlParameter("@Action", "SelectDepartments")
                });

            model.Departments = dtDept.AsEnumerable()
                .Select(r => new Department
                {
                    DepartmentId = Convert.ToInt32(r["DepartmentId"]),
                    DepartmentName = r["DepartmentName"].ToString()
                }).ToList();

            if (id == null)
            {
                model.IsActive = true;
                return View(model);
            }

            // Edit Doctor
            SqlParameter[] param = {
        new SqlParameter("@Action", "SelectDoctorById"),
        new SqlParameter("@DoctorId", id)
    };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageDoctor", param);

            if (dt.Rows.Count > 0)
            {
                var r = dt.Rows[0];
                model.DoctorId = Convert.ToInt32(r["DoctorId"]);
                model.DepartmentId = Convert.ToInt32(r["DepartmentId"]);
                model.FullName = r["FullName"].ToString();
                model.Email = r["Email"].ToString();
                model.MobileNu = r["MobileNu"].ToString();
                model.Address = r["Address"].ToString();
                model.IsActive = Convert.ToBoolean(r["IsActive"]);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddDoctor(Doctor model)
        {
            string action = model.DoctorId == 0 ? "InsertDoctor" : "UpdateDoctor";

            SqlParameter[] parameters =
            {
        new SqlParameter("@Action", action),
        new SqlParameter("@DoctorId", model.DoctorId),
        new SqlParameter("@DepartmentId", model.DepartmentId),
        new SqlParameter("@FullName", model.FullName),
        new SqlParameter("@Email", model.Email),
        new SqlParameter("@MobileNu", model.MobileNu),
        new SqlParameter("@Address", model.Address),
        new SqlParameter("@IsActive", model.IsActive)
         };

            await _dbLayer.ExecuteSPAsync("sp_ManageDoctor", parameters);

            return RedirectToAction("DoctorsList");
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
        public async Task<IActionResult> ManageNurse(int id, string actionType)
        {
            string spAction = actionType switch
            {
                "Delete" => "Delete",
                "ToggleStatus" => "ToggleStatus",
                _ => null
            };

            if (spAction == null) return BadRequest();

            await _dbLayer.ExecuteSPAsync("sp_ManageNurse", new SqlParameter[]
            {
        new SqlParameter("@Action", spAction),
        new SqlParameter("@NurseId", id)
            });

            return RedirectToAction("NurseList");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedNurses(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManageNurse", new SqlParameter[]
                    {
                new SqlParameter("@Action", "Delete"),
                new SqlParameter("@NurseId", id)
                    });
                }
            }
            return RedirectToAction("NurseList");
        }

        [HttpGet]
        public async Task<IActionResult> AddNurse(int? id)
        {
            Nurse model = new Nurse { IsActive = true };

            if (id == null) return View(model);

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageNurse", new SqlParameter[]
            {
        new SqlParameter("@Action", "SelectById"),
        new SqlParameter("@NurseId", id)
            });

            if (dt.Rows.Count == 0) return NotFound();

            var r = dt.Rows[0];
            model.NurseId = Convert.ToInt32(r["NurseId"]);
            model.FullName = r["FullName"].ToString();
            model.Email = r["Email"].ToString();
            model.MobileNu = r["MobileNu"].ToString();
            model.IsActive = Convert.ToBoolean(r["IsActive"]);
            model.Password = r["Password"].ToString();
            model.ProfileImagePath = r.Table.Columns.Contains("ProfileImagePath")
                                     ? r["ProfileImagePath"].ToString()
                                     : null;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddNurse(Nurse model, IFormFile ProfileImage)
        {
            if (!ModelState.IsValid) return View(model);
            ModelState.Remove("ProfileImage");
            // Handle Profile Image
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                string folder = Path.Combine("wwwroot", "uploads", "nurses");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(ProfileImage.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(stream);
                }

                // Save relative path for DB
                model.ProfileImagePath = "/uploads/nurses/" + fileName;
            }

            string action = model.NurseId == 0 ? "Insert" : "Update";

            // **Password update logic**: only update if user entered a new password
            SqlParameter passwordParam = new SqlParameter("@Password", SqlDbType.NVarChar);
            if (!string.IsNullOrEmpty(model.Password))
                passwordParam.Value = model.Password;
            else
                passwordParam.Value = DBNull.Value; // leave it as null if not changed

            SqlParameter[] parameters =
            {
        new SqlParameter("@Action", action),
        new SqlParameter("@NurseId", model.NurseId),
        new SqlParameter("@FullName", model.FullName),
        new SqlParameter("@Email", model.Email),
        new SqlParameter("@MobileNu", model.MobileNu),
        new SqlParameter("@IsActive", model.IsActive),
        passwordParam,
        new SqlParameter("@ProfileImagePath", model.ProfileImagePath ?? (object)DBNull.Value)
    };

            await _dbLayer.ExecuteSPAsync("sp_ManageNurse", parameters);

            return RedirectToAction("NurseList");
        }



    }
}
