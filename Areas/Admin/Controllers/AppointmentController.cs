using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HMSCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AppointmentController : BaseController
    {
    
            private readonly IDbLayer _dbLayer;

            public AppointmentController(IDbLayer dbLayer)
            {
                _dbLayer = dbLayer;
            }

        [HttpGet]
        public async Task<IActionResult> DoctorSlot(
          string filterColumn = null,
          string keyword = null,
          string fromDate = null,
          string toDate = null,
          int pageSize = 20)
        {
            // Prepare parameters for your stored procedure
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Action", "SelectDoctorSlots"),
        new SqlParameter("@FilterColumn", string.IsNullOrEmpty(filterColumn) ? DBNull.Value : (object)filterColumn),
        new SqlParameter("@Keyword", string.IsNullOrEmpty(keyword) ? DBNull.Value : (object)keyword),
        new SqlParameter("@FromDate", string.IsNullOrEmpty(fromDate) ? DBNull.Value : (object)fromDate),
        new SqlParameter("@ToDate", string.IsNullOrEmpty(toDate) ? DBNull.Value : (object)toDate)
            };

            // Execute stored procedure
            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_ManageDoctorSlots", parameters);

            // Map DataTable to model
            var slots = dt.AsEnumerable().Select(r => new DoctorSlot
            {
                ScheduleId = Convert.ToInt32(r["ScheduleId"]),
                DepartmentName = r["DepartmentName"].ToString(),
                FullName = r["DoctorName"].ToString(),
                ScheduleDate = Convert.ToDateTime(r["ScheduleDate"]),
                StartTime = r["StartTime"].ToString(),
                EndTime = r["EndTime"].ToString(),
                SlotDuration = r["SlotDuration"].ToString(),
                SessionType = r["SessionType"].ToString(),
                IsBlocked = Convert.ToBoolean(r["IsBlocked"])
            }).ToList();

            // Fill ViewModel for the UI
            var vm = new DoctorSlotViewModel
            {
                PageSize = pageSize,
                FilterColumn = filterColumn,
                Keyword = keyword,
                FromDate = string.IsNullOrEmpty(fromDate) ? (DateTime?)null : DateTime.Parse(fromDate),
                ToDate = string.IsNullOrEmpty(toDate) ? (DateTime?)null : DateTime.Parse(toDate),
                Slots = slots
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleBlock(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_ManageDoctorSlots", new[]
            {
        new SqlParameter("@Action", "ToggleBlock"),
        new SqlParameter("@ScheduleId", id)
    });

            TempData["Message"] = "Slot status updated successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("DoctorSlot");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSlot(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_ManageDoctorSlots", new[]
            {
        new SqlParameter("@Action", "DeleteSlot"),
        new SqlParameter("@ScheduleId", id)
    });

            TempData["Message"] = "Slot deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("DoctorSlot");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelected(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_ManageDoctorSlots", new[]
                    {
                new SqlParameter("@Action", "DeleteSlot"),
                new SqlParameter("@ScheduleId", id)
            });
                }

                TempData["Message"] = "Selected slots deleted successfully";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "No slots selected";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("DoctorSlot");
        }


        [HttpGet]
        public async Task<IActionResult> SearchSlot(
            int? DepartmentId,
            int? DoctorId,
            DateTime? FromDate,
            DateTime? ToDate,
            string ShowMorning = "true",
            string ShowAfternoon = "true",
            string ShowEvening = "true")
        {
            var vm = new DoctorSlotViewModel
            {
                DepartmentId = DepartmentId,
                DoctorId = DoctorId,
                FromDate = FromDate,
                ToDate = ToDate
            };

            vm.SetSessionFilters(ShowMorning, ShowAfternoon, ShowEvening);

            // Departments
            var dtDept = await _dbLayer.ExecuteSPAsync("spSearchDoctorSlots",
                new[] { new SqlParameter("@Action", "GetDepartments") });

            vm.Departments = dtDept.AsEnumerable()
                .Select(r => new Department
                {
                    DepartmentId = Convert.ToInt32(r["DepartmentId"]),
                    DepartmentName = r["DepartmentName"].ToString()
                }).ToList();

            // Doctors if Department selected
            if (DepartmentId.HasValue)
            {
                var dtDoc = await _dbLayer.ExecuteSPAsync("spSearchDoctorSlots",
                    new[]
                    {
                new SqlParameter("@Action", "GetDoctors"),
                new SqlParameter("@DepartmentId", DepartmentId.Value)
                    });

                vm.Doctors = dtDoc.AsEnumerable()
                    .Select(r => new Doctor
                    {
                        DoctorId = Convert.ToInt32(r["DoctorId"]),
                        FullName = r["FullName"].ToString()
                    }).ToList();
            }

            await PopulateSlots(vm);

            return View(vm);
        }

        [HttpPost]
     public async Task<IActionResult> ToggleBlockTimeSlot(
      int doctorId,
      DateTime slotDate,
      string slotTime,
      int? DepartmentId,
      int? DoctorId,
      DateTime? FromDate,
      DateTime? ToDate,
      string ShowMorning = "true",
      string ShowAfternoon = "true",
      string ShowEvening = "true")
        {
            // 1️⃣ Toggle the slot
            TimeSpan time = TimeSpan.Parse(slotTime);

            await _dbLayer.ExecuteSPAsync(
                "spSearchDoctorSlots",
                new[] {
            new SqlParameter("@Action", "ToggleBlock"),
            new SqlParameter("@DoctorId", doctorId),
            new SqlParameter("@SlotDate", slotDate.Date),
            new SqlParameter("@SlotTime", time)
                });

            TempData["Message"] = "Slot status updated.";
            TempData["MessageType"] = "success";
            // 2️⃣ Redirect to SearchSlot with all filters preserved
            return RedirectToAction("SearchSlot", new
            {
                DepartmentId = DepartmentId,
                DoctorId = DoctorId,
                FromDate = FromDate?.ToString("yyyy-MM-dd"),
                ToDate = ToDate?.ToString("yyyy-MM-dd"),
                ShowMorning,
                ShowAfternoon,
                ShowEvening
            });
        }




        private async Task PopulateSlots(DoctorSlotViewModel vm)
        {
            vm.Slots.Clear();

            if (!vm.DoctorId.HasValue || !vm.FromDate.HasValue || !vm.ToDate.HasValue)
                return;

            var dtSlots = await _dbLayer.ExecuteSPAsync(
                "spSearchDoctorSlots",
                new[]
                {
            new SqlParameter("@Action", "GetSlots"),
            new SqlParameter("@DoctorId", vm.DoctorId.Value),
            new SqlParameter("@FromDate", vm.FromDate.Value),
            new SqlParameter("@ToDate", vm.ToDate.Value)
                });

            // Get blocked slots
            var dtBlocked = await _dbLayer.ExecuteSPAsync(
                "spSearchDoctorSlots",
                new[]
                {
            new SqlParameter("@Action", "GetBlocked"),
            new SqlParameter("@DoctorId", vm.DoctorId.Value),
            new SqlParameter("@FromDate", vm.FromDate.Value),
            new SqlParameter("@ToDate", vm.ToDate.Value)
                });

            var blockedSlots = dtBlocked.AsEnumerable()
                .Where(r => Convert.ToBoolean(r["IsActive"]))
                .Select(r => new { Date = Convert.ToDateTime(r["SlotDate"]).Date, Time = (TimeSpan)r["SlotTime"] })
                .ToList();

            foreach (DataRow r in dtSlots.Rows)
            {
                string session = r["SessionType"].ToString();
                 
                // Apply session filters using string-based boolean helpers
                if ((session == "Morning" && !vm.IsMorning) ||
                    (session == "Afternoon" && !vm.IsAfternoon) ||
                    (session == "Evening" && !vm.IsEvening))
                    continue;


                DateTime scheduleDate = Convert.ToDateTime(r["ScheduleDate"]);
                TimeSpan start = (TimeSpan)r["StartTime"];
                TimeSpan end = (TimeSpan)r["EndTime"];
                int duration = Convert.ToInt32(r["SlotDuration"]);

                for (DateTime t = scheduleDate.Date + start; t < scheduleDate.Date + end; t = t.AddMinutes(duration))
                {
                    bool isBlocked = blockedSlots.Any(b => b.Date == scheduleDate.Date && b.Time == t.TimeOfDay);

                    vm.Slots.Add(new DoctorSlot
                    {
                        ScheduleId = Convert.ToInt32(r["ScheduleId"]),
                        DoctorId = Convert.ToInt32(r["DoctorId"]),
                        DepartmentName = r["DepartmentName"].ToString(),
                        FullName = r["DoctorName"].ToString(),
                        ScheduleDate = scheduleDate,
                        SlotTime = t.ToString("hh:mm tt"),
                        SlotDuration = duration.ToString(),
                        SessionType = session,
                        IsBlocked = isBlocked
                    });
                }
            }
        }




        [HttpGet]
        public async Task<IActionResult> UnblockSlot(
      int? DepartmentId,
      int? DoctorId,
      DateTime? FromDate,
      DateTime? ToDate,
      string ShowMorning = "true",
      string ShowAfternoon = "true",
      string ShowEvening = "true")
        {
            var vm = new DoctorSlotViewModel
            {
                DepartmentId = DepartmentId,
                DoctorId = DoctorId,
                FromDate = FromDate,
                ToDate = ToDate
            };

            vm.SetSessionFilters(ShowMorning, ShowAfternoon, ShowEvening);

            // 1️⃣ Get Departments
            var dtDept = await _dbLayer.ExecuteSPAsync("spSearchUnblockDoctorSlots",
                new[] { new SqlParameter("@Action", "GetDepartments") });

            vm.Departments = dtDept.AsEnumerable()
                .Select(r => new Department
                {
                    DepartmentId = Convert.ToInt32(r["DepartmentId"]),
                    DepartmentName = r["DepartmentName"].ToString()
                }).ToList();

            // 2️⃣ Get Doctors for selected Department
            if (DepartmentId.HasValue)
            {
                var dtDoc = await _dbLayer.ExecuteSPAsync("spSearchUnblockDoctorSlots",
                    new[]
                    {
                new SqlParameter("@Action", "GetDoctors"),
                new SqlParameter("@DepartmentId", DepartmentId.Value)
                    });

                vm.Doctors = dtDoc.AsEnumerable()
                    .Select(r => new Doctor
                    {
                        DoctorId = Convert.ToInt32(r["DoctorId"]),
                        FullName = r["FullName"].ToString()
                    }).ToList();
            }

            // 3️⃣ Populate only blocked slots
            await PopulateUnblockSlots(vm);

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUnblockTimeSlot(
            int doctorId,
            DateTime slotDate,
            string slotTime,
            int? DepartmentId,
            int? DoctorId,
            DateTime? FromDate,
            DateTime? ToDate,
            string ShowMorning = "true",
            string ShowAfternoon = "true",
            string ShowEvening = "true")
        {
            // Toggle blocked slot
            TimeSpan time = TimeSpan.Parse(slotTime);

            await _dbLayer.ExecuteSPAsync(
                "spSearchUnblockDoctorSlots",
                new[]
                {
            new SqlParameter("@Action", "ToggleBlock"),
            new SqlParameter("@DoctorId", doctorId),
            new SqlParameter("@SlotDate", slotDate.Date),
            new SqlParameter("@SlotTime", time)
                });

            TempData["Message"] = "Slot status updated."; 
            TempData["MessageType"] = "success";
            // Redirect back to GET with filters preserved
            return RedirectToAction("UnblockSlot", new
            {
                DepartmentId,
                DoctorId,
                FromDate = FromDate?.ToString("yyyy-MM-dd"),
                ToDate = ToDate?.ToString("yyyy-MM-dd"),
                ShowMorning,
                ShowAfternoon,
                ShowEvening
            });
        }
        private async Task PopulateUnblockSlots(DoctorSlotViewModel vm)
        {
            vm.Slots.Clear();

            if (!vm.DoctorId.HasValue || !vm.FromDate.HasValue || !vm.ToDate.HasValue)
                return;

            // Get blocked slots (SP should return SlotDuration now)
            var dtBlocked = await _dbLayer.ExecuteSPAsync(
                "spSearchUnblockDoctorSlots",
                new[]
                {
            new SqlParameter("@Action", "GetBlockedSlots"),
            new SqlParameter("@DoctorId", vm.DoctorId.Value),
            new SqlParameter("@FromDate", vm.FromDate.Value),
            new SqlParameter("@ToDate", vm.ToDate.Value)
                });

            vm.Slots = dtBlocked.AsEnumerable()
                .Select(r =>
                {
                    string session = r["SessionType"].ToString();

                    // Apply session filters
                    if ((session == "Morning" && !vm.IsMorning) ||
                        (session == "Afternoon" && !vm.IsAfternoon) ||
                        (session == "Evening" && !vm.IsEvening))
                        return null;

                    return new DoctorSlot
                    {
                        ScheduleId = Convert.ToInt32(r["BlockId"]),
                        DoctorId = Convert.ToInt32(r["DoctorId"]),
                        DepartmentName = r["DepartmentName"].ToString(),
                        FullName = r["DoctorName"].ToString(),
                        ScheduleDate = Convert.ToDateTime(r["SlotDate"]),
                        SlotTime = ((TimeSpan)r["SlotTime"]).ToString(@"hh\:mm"),
                        SlotDuration = r["SlotDuration"] != DBNull.Value ? r["SlotDuration"].ToString() : "N/A",
                        SessionType = session,
                        IsBlocked = Convert.ToBoolean(r["IsActive"])
                    };
                })
                .Where(s => s != null)
                .ToList();
        }

 
            [HttpGet]
            public async Task<IActionResult> AppointmentReport(
                string filterColumn,
                string keyword,
                DateTime? fromDate,
                DateTime? toDate,
                int pageNumber = 1,
                int pageSize = 20)
                {
                var vm = new AppointmentReportVM
                {
                    FilterColumn = filterColumn,
                    Keyword = keyword,
                    FromDate = fromDate,
                    ToDate = toDate,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var dt = await _dbLayer.ExecuteSPAsync("sp_AppointmentReport", new[]
                {
                new SqlParameter("@Action","GetPaged"),
                new SqlParameter("@FilterColumn",(object?)filterColumn ?? DBNull.Value),
                new SqlParameter("@FilterValue",(object?)keyword ?? DBNull.Value),
                new SqlParameter("@FromDate",(object?)fromDate ?? DBNull.Value),
                new SqlParameter("@ToDate",(object?)toDate ?? DBNull.Value),
                new SqlParameter("@PageNumber",pageNumber),
                new SqlParameter("@PageSize",pageSize)
            });

                if (dt.Rows.Count > 0)
                    vm.TotalRecords = Convert.ToInt32(dt.Rows[0]["TotalRecords"]);

                vm.Appointments = dt.AsEnumerable().Select(r => new AppointmentReportViewModel
                {
                    BookingId = Convert.ToInt32(r["BookingId"]),
                    AppointmentId = Convert.ToInt32(r["AppointmentId"]),
                    PatientName = r["PatientName"].ToString(),
                    Phone = r["Phone"].ToString(),
                    Email = r["Email"].ToString(),
                    DoctorName = r["DoctorName"].ToString(),
                    DepartmentName = r["DepartmentName"].ToString(),
                    AppointmentDate = Convert.ToDateTime(r["AppointmentDate"]),
                    AppointmentTime = (TimeSpan)r["AppointmentTime"],
                    Status = r["Status"].ToString()
                }).ToList();

                return View(vm);
            }

            [HttpPost]
            public async Task<IActionResult> ToggleAppointmentStatus(int id)
            {
                await _dbLayer.ExecuteSPAsync("sp_AppointmentReport", new[]
                {
                new SqlParameter("@Action","ToggleStatus"),
                new SqlParameter("@AppointmentId", id)
            });

                TempData["Message"] = "Status updated successfully";
            TempData["MessageType"] = "success";
            return RedirectToAction("AppointmentReport");
            }

            [HttpPost]
            public async Task<IActionResult> DeleteAppointment(int id)
            {
                await _dbLayer.ExecuteSPAsync("sp_AppointmentReport", new[]
                {
                new SqlParameter("@Action","Delete"),
                new SqlParameter("@AppointmentId", id)
            });

                TempData["Message"] = "Appointment deleted successfully";
            TempData["MessageType"] = "success";
            return RedirectToAction("AppointmentReport");
            }

            // DELETE SELECTED APPOINTMENTS
            [HttpPost]
            public async Task<IActionResult> DeleteSelectedAppointment(string selectedIds)
            {
                if (!string.IsNullOrWhiteSpace(selectedIds))
                {
                    var ids = selectedIds.Split(',').Select(id => new SqlParameter("@AppointmentId", int.Parse(id))).ToArray();
                    foreach (var param in ids)
                    {
                        await _dbLayer.ExecuteSPAsync("sp_AppointmentReport", new[]
                        {
                        new SqlParameter("@Action","Delete"),
                        param
                    });
                    }
                    TempData["Message"] = "Selected appointments deleted successfully";
                TempData["MessageType"] = "success";
            }
                else
                {
                TempData["Message"] = "No appointments selected";
                TempData["MessageType"] = "error";
                 
                }

                return RedirectToAction("AppointmentReport");
            }



        [HttpGet]
        public async Task<IActionResult> TodayPatientAppointment(
          string filterColumn,
          string keyword,
          DateTime? fromDate,
          DateTime? toDate,
          int pageNumber = 1,
          int pageSize = 20)
        {
            var vm = new AppointmentReportVM
            {
                FilterColumn = filterColumn,
                Keyword = keyword,
                FromDate = fromDate,
                ToDate = toDate,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var dt = await _dbLayer.ExecuteSPAsync("sp_TodayAppointmentReport", new[]
            {
        new SqlParameter("@Action","GetPaged"),
        new SqlParameter("@FilterColumn",(object?)filterColumn ?? DBNull.Value),
        new SqlParameter("@FilterValue",(object?)keyword ?? DBNull.Value),
        new SqlParameter("@FromDate",(object?)fromDate ?? DBNull.Value),
        new SqlParameter("@ToDate",(object?)toDate ?? DBNull.Value),
        new SqlParameter("@PageNumber",pageNumber),
        new SqlParameter("@PageSize",pageSize)
    });

            if (dt.Rows.Count > 0)
                vm.TotalRecords = Convert.ToInt32(dt.Rows[0]["TotalRecords"]);

            vm.Appointments = dt.AsEnumerable().Select(r => new AppointmentReportViewModel
            {
                BookingId = Convert.ToInt32(r["BookingId"]),
                AppointmentId = Convert.ToInt32(r["AppointmentId"]),
                PatientName = r["PatientName"].ToString(),
                Phone = r["Phone"].ToString(),
                Email = r["Email"].ToString(),
                DoctorName = r["DoctorName"].ToString(),
                DepartmentName = r["DepartmentName"].ToString(),
                AppointmentDate = Convert.ToDateTime(r["AppointmentDate"]),
                AppointmentTime = (TimeSpan)r["AppointmentTime"],
                Status = r["Status"].ToString()
            }).ToList();

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleTodayAppointmentStatus(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_TodayAppointmentReport", new[]
            {
        new SqlParameter("@Action","ToggleStatus"),
        new SqlParameter("@AppointmentId", id)
    });

            TempData["Message"] = "Status updated successfully";
            TempData["MessageType"] = "success";
            return RedirectToAction("TodayPatientAppointment");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTodayAppointment(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_TodayAppointmentReport", new[]
            {
        new SqlParameter("@Action","Delete"),
        new SqlParameter("@AppointmentId", id)
    });

            TempData["Message"] = "Appointment deleted successfully";
            TempData["MessageType"] = "success";
            return RedirectToAction("TodayPatientAppointment");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedTodayAppointment(string selectedIds)
        {
            if (!string.IsNullOrWhiteSpace(selectedIds))
            {
                var ids = selectedIds.Split(',').Select(id => new SqlParameter("@AppointmentId", int.Parse(id))).ToArray();
                foreach (var param in ids)
                {
                    await _dbLayer.ExecuteSPAsync("sp_TodayAppointmentReport", new[]
                    {
                new SqlParameter("@Action","Delete"),
                param
            });
                }
                TempData["Message"] = "Selected appointments deleted successfully";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "No appointments selected";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("TodayPatientAppointment");
        }

    }
}
