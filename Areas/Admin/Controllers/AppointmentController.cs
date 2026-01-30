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
                }
                return RedirectToAction("DoctorSlot");
            }




        [HttpGet]
        public async Task<IActionResult> SearchSlot(
       int? DepartmentId,
       int? DoctorId,
       DateTime? FromDate,
       DateTime? ToDate,
       string IsMorning = "true",
       string IsAfternoon = "true",
       string IsEvening = "true")
        {
            var vm = new DoctorSlotViewModel
            {
                DepartmentId = DepartmentId,
                DoctorId = DoctorId,
                FromDate = FromDate,
                ToDate = ToDate
                // ❌ IsMorning / IsAfternoon / IsEvening assign NAHI karna
            };

            // Departments
            var dtDept = await _dbLayer.ExecuteSPAsync(
                "spSearchDoctorSlots",
                new[] { new SqlParameter("@Action", "GetDepartments") });

            vm.Departments = dtDept.AsEnumerable()
                .Select(r => new Department
                {
                    DepartmentId = Convert.ToInt32(r["DepartmentId"]),
                    DepartmentName = r["DepartmentName"].ToString()
                }).ToList();

            // Doctors (IMPORTANT – warna dropdown blank rahega)
            if (DepartmentId.HasValue)
            {
                var dtDoc = await _dbLayer.ExecuteSPAsync(
                    "spSearchDoctorSlots",
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
            // Parse slot time
            TimeSpan time = TimeSpan.Parse(slotTime);

            // Toggle block using SP
            await _dbLayer.ExecuteSPAsync(
                "spSearchDoctorSlots",
                new[]
                {
            new SqlParameter("@Action", SqlDbType.NVarChar) { Value = "ToggleBlock" },
            new SqlParameter("@DoctorId", SqlDbType.Int) { Value = doctorId },
            new SqlParameter("@SlotDate", SqlDbType.Date) { Value = slotDate.Date },
            new SqlParameter("@SlotTime", SqlDbType.Time) { Value = time }
                });

            TempData["Message"] = "Slot status updated.";

            // Rebuild model and preserve session filters
            var vm = new DoctorSlotViewModel
            {
                DepartmentId = DepartmentId,
                DoctorId = DoctorId,
                FromDate = FromDate,
                ToDate = ToDate,
                ShowMorning = ShowMorning,
                ShowAfternoon = ShowAfternoon,
                ShowEvening = ShowEvening
            };

            await PopulateSlots(vm);

            ViewBag.Message = TempData["Message"];

            return View("SearchSlot", vm);
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




        public IActionResult UnblockSlot()
        {
            return View();
        }


        public IActionResult AppointmentReport()
        {
            return View();
        }
        public IActionResult TodayPatientAppointment()
        {
            return View();
        }
    }
}
