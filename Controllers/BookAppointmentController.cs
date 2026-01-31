using HMSCore.Data;
using HMSCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace HMSCore.Controllers
{
    public class BookAppointmentController : Controller
    {
        private readonly IDbLayer _dbLayer;

        public BookAppointmentController(IDbLayer dbLayer)
        {
            _dbLayer = dbLayer;
        }

        [HttpGet]
        public async Task<IActionResult> BookAppointment()
        {
            var model = new AppointmentViewModel();

            // Dummy output param to satisfy SP
            var outputParam = new SqlParameter("@BookingID", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Output };

            // Get Departments
            DataTable dtDepartments = await _dbLayer.ExecuteSPAsync("sp_ManageAppointments", new[]
            {
                new SqlParameter("@Mode", "GetDepartments"),
                outputParam
            });

            model.Departments = new List<Department>();
            foreach (DataRow row in dtDepartments.Rows)
            {
                model.Departments.Add(new Department
                {
                    DepartmentId = Convert.ToInt32(row["DepartmentId"]),
                    DepartmentName = Convert.ToString(row["DepartmentName"])
                });
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctors(int departmentId)
        {
            try
            {
                DataTable dtDoctors = await _dbLayer.ExecuteSPAsync("sp_ManageAppointments", new[]
                {
            new SqlParameter("@Mode", "GetDoctors"),
            new SqlParameter("@DepartmentId", departmentId)
        });

                if (dtDoctors == null || dtDoctors.Rows.Count == 0)
                    return Json(new List<Doctor>()); // return empty list if no doctors

                var doctors = dtDoctors.AsEnumerable()
                                       .Select(row => new Doctor
                                       {
                                           DoctorId = Convert.ToInt32(row["DoctorId"]),
                                           FullName = row["FullName"].ToString(),
                                           Address = row["Address"] != DBNull.Value ? row["Address"].ToString() : string.Empty
                                       })
                                       .ToList();

                return Json(doctors);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetSchedules(int doctorId, DateTime date)
        {
            try
            {
                // 1️⃣ Get doctor schedule
                DataTable dtSchedules = await _dbLayer.ExecuteSPAsync(
                    "sp_ManageAppointments",
                    new[]
                    {
                new SqlParameter("@Mode", "GetSchedules"),
                new SqlParameter("@DoctorId", doctorId),
                new SqlParameter("@ScheduleDate", date.Date)
                    });

                // 2️⃣ Get blocked slots
                DataTable dtBlocked = await _dbLayer.ExecuteSPAsync(
                    "sp_ManageAppointments",
                    new[]
                    {
                new SqlParameter("@Mode", "GetBlockedSlots"),
                new SqlParameter("@DoctorId", doctorId),
                new SqlParameter("@ScheduleDate", date.Date)
                    });

                // 3️⃣ Get booked appointments
                DataTable dtBooked = await _dbLayer.ExecuteSPAsync(
                    "sp_ManageAppointments",
                    new[]
                    {
                new SqlParameter("@Mode", "GetAppointments"),
                new SqlParameter("@DoctorId", doctorId),
                new SqlParameter("@ScheduleDate", date.Date)
                    });

                // 4️⃣ Prepare slot collections
                HashSet<string> blockedSlots = new();
                foreach (DataRow row in dtBlocked.Rows)
                {
                    if (row["SlotTime"] != DBNull.Value)
                        blockedSlots.Add(Convert.ToDateTime(row["SlotTime"]).ToString("HH:mm"));
                }

                HashSet<string> bookedSlots = new();
                foreach (DataRow row in dtBooked.Rows)
                {
                    if (row["AppointmentTime"] != DBNull.Value)
                        bookedSlots.Add(Convert.ToDateTime(row["AppointmentTime"]).ToString("HH:mm"));
                }

                List<string> available = new();
                List<string> timeout = new();

                // 5️⃣ If no schedule found
                if (dtSchedules == null || dtSchedules.Rows.Count == 0)
                {
                    return Json(new
                    {
                        available,
                        booked = bookedSlots,
                        timeout,
                        blocked = blockedSlots
                    });
                }

                // 6️⃣ Generate slots safely
                foreach (DataRow row in dtSchedules.Rows)
                {
                    if (row["StartTime"] == DBNull.Value ||
                        row["EndTime"] == DBNull.Value ||
                        row["SlotDuration"] == DBNull.Value)
                        continue;

                    // TIME column → TimeSpan
                    TimeSpan startTs = (TimeSpan)row["StartTime"];
                    TimeSpan endTs = (TimeSpan)row["EndTime"];

                    DateTime startTime = date.Date.Add(startTs);
                    DateTime endTime = date.Date.Add(endTs);

                    int slotDuration = Convert.ToInt32(row["SlotDuration"]);

                    bool isBlocked = row["IsBlocked"] != DBNull.Value &&
                                     Convert.ToInt32(row["IsBlocked"]) == 1;

                    for (DateTime t = startTime; t < endTime; t = t.AddMinutes(slotDuration))
                    {
                        string slotStr = t.ToString("hh:mm tt"); // ✅ 12-hour format

                        if (isBlocked || blockedSlots.Contains(slotStr))
                            continue;

                        if (bookedSlots.Contains(slotStr))
                            continue;

                        if (date.Date == DateTime.Today && t < DateTime.Now)
                            timeout.Add(slotStr);
                        else
                            available.Add(slotStr);
                    }

                }


                // 7️⃣ Final response
                return Json(new
                {
                    available,
                    booked = bookedSlots,
                    timeout,
                    blocked = blockedSlots
                });
            }
            catch (Exception ex)
            {
                // 8️⃣ Error handling (debug friendly)
                return Json(new
                {
                    error = true,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BookAppointment([FromBody] AppointmentViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");

            try
            {
                var outputParam = new SqlParameter("@BookingID", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Output };

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@Mode", "BookAppointment"),
                    new SqlParameter("@DoctorId", model.SelectedDoctorId ?? 0),
                    new SqlParameter("@ScheduleDate", model.SelectedDate ?? DateTime.Today),
                    new SqlParameter("@AppointmentTime", model.SelectedSlot ?? ""),
                    new SqlParameter("@PatientName", model.PatientName ?? ""),
                    new SqlParameter("@Phone", model.Phone ?? ""),
                    new SqlParameter("@Email", model.Email ?? ""),
                    new SqlParameter("@Message", model.Message ?? ""),
                    outputParam
                };

                await _dbLayer.ExecuteSPAsync("sp_ManageAppointments", parameters);

                string bookingId = Convert.ToString(outputParam.Value);

                return Json(new { success = true, bookingId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
