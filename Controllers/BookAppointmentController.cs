using HMSCore.Data;
using HMSCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Mail;
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
                DataTable dtSchedules = await _dbLayer.ExecuteSPAsync(
                    "sp_ManageAppointments",
                    new[]
                    {
                new SqlParameter("@Mode", "GetSchedules"),
                new SqlParameter("@DoctorId", doctorId),
                new SqlParameter("@ScheduleDate", date.Date)
                    });

                DataTable dtBlocked = await _dbLayer.ExecuteSPAsync(
                    "sp_ManageAppointments",
                    new[]
                    {
                new SqlParameter("@Mode", "GetBlockedSlots"),
                new SqlParameter("@DoctorId", doctorId),
                new SqlParameter("@ScheduleDate", date.Date)
                    });

                DataTable dtBooked = await _dbLayer.ExecuteSPAsync(
                    "sp_ManageAppointments",
                    new[]
                    {
                new SqlParameter("@Mode", "GetAppointments"),
                new SqlParameter("@DoctorId", doctorId),
                new SqlParameter("@ScheduleDate", date.Date)
                    });

                /* =========================
                   BLOCKED SLOTS (SAFE)
                ========================== */
                HashSet<string> blockedSlots = new();
                foreach (DataRow row in dtBlocked.Rows)
                {
                    if (row["SlotTime"] == DBNull.Value) continue;

                    if (row["SlotTime"] is TimeSpan ts)
                    {
                        blockedSlots.Add(ts.ToString(@"hh\:mm"));
                    }
                    else
                    {
                        if (TimeSpan.TryParse(row["SlotTime"].ToString(), out TimeSpan parsed))
                            blockedSlots.Add(parsed.ToString(@"hh\:mm"));
                    }
                }

                /* =========================
                   BOOKED SLOTS (SAFE)
                ========================== */
                HashSet<string> bookedSlots = new();
                foreach (DataRow row in dtBooked.Rows)
                {
                    if (row["AppointmentTime"] == DBNull.Value) continue;

                    if (row["AppointmentTime"] is TimeSpan ts)
                    {
                        bookedSlots.Add(ts.ToString(@"hh\:mm"));
                    }
                    else
                    {
                        if (TimeSpan.TryParse(row["AppointmentTime"].ToString(), out TimeSpan parsed))
                            bookedSlots.Add(parsed.ToString(@"hh\:mm"));
                    }
                }

                List<string> available = new();
                List<string> timeout = new();

                /* =========================
                   NO SCHEDULE
                ========================== */
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

                /* =========================
                   GENERATE SLOTS
                ========================== */
                foreach (DataRow row in dtSchedules.Rows)
                {
                    if (row["StartTime"] == DBNull.Value ||
                        row["EndTime"] == DBNull.Value ||
                        row["SlotDuration"] == DBNull.Value)
                        continue;

                    TimeSpan startTs = (TimeSpan)row["StartTime"];
                    TimeSpan endTs = (TimeSpan)row["EndTime"];

                    DateTime startTime = date.Date.Add(startTs);
                    DateTime endTime = date.Date.Add(endTs);

                    int slotDuration = Convert.ToInt32(row["SlotDuration"]);

                    bool isBlockedSchedule =
                        row["IsBlocked"] != DBNull.Value &&
                        Convert.ToInt32(row["IsBlocked"]) == 1;

                    for (DateTime t = startTime; t < endTime; t = t.AddMinutes(slotDuration))
                    {
                        string slotStr = t.ToString("HH:mm"); // frontend-safe

                        if (isBlockedSchedule || blockedSlots.Contains(slotStr))
                            continue;

                        if (bookedSlots.Contains(slotStr))
                            continue;

                        if (date.Date == DateTime.Today && t < DateTime.Now)
                            timeout.Add(slotStr);
                        else
                            available.Add(slotStr);
                    }
                }

                /* =========================
                   FINAL RESPONSE
                ========================== */
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
                // Output parameter for Booking ID
                var outputParam = new SqlParameter("@BookingID", SqlDbType.NVarChar, 50)
                {
                    Direction = ParameterDirection.Output
                };

                // SP parameters
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

                // Execute stored procedure
                await _dbLayer.ExecuteSPAsync("sp_ManageAppointments", parameters);

                // Get generated Booking ID
                string bookingId = Convert.ToString(outputParam.Value);

                // Send booking confirmation email
                if (!string.IsNullOrEmpty(model.Email))
                {
                    SendBookingConfirmationEmail(model.Email, model.PatientName, bookingId, model.SelectedDate ?? DateTime.Today, model.SelectedSlot ?? "");
                }

                return Json(new { success = true, bookingId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // === Helper method for sending email ===
        private static void SendBookingConfirmationEmail(string toEmail, string patientName, string bookingId, DateTime apptDate, string slotTime)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(toEmail);
                mail.From = new MailAddress("trianglemind14@gmail.com");
                mail.Subject = "Appointment Confirmation - TriangleMind";

                string body = string.Format(@"
<html>
  <body style='font-family: Arial, sans-serif; font-size: 14px; color: black;'>
    <div style='text-align: center; margin-bottom: 20px;'>
      <img src='https://www.trianglemind.in/assets/TMT_img/logo.png' alt='Logo' style='height: 80px;' />
    </div>
    <p>Dear {0},</p>
    <p>Thank you for booking your appointment with <strong>Triangle Mind</strong>.</p>

    <p><b>Booking Details:</b></p>
    <table style='border-collapse: collapse;'>
      <tr><td><b>Booking ID:</b></td><td>{1}</td></tr>
      <tr><td><b>Date:</b></td><td>{2:dddd, dd MMM yyyy}</td></tr>
      <tr><td><b>Appointment Time:</b></td><td>{3}</td></tr>
    </table>

    <p>We look forward to seeing you.</p>
    <p style='color:gray; font-size:12px;'>This is an automated email, please do not reply.</p>
  </body>
</html>", patientName, bookingId, apptDate, slotTime);

                mail.Body = body;
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("trianglemind14@gmail.com", "mgae pptn cmej axlp"),
                    EnableSsl = true
                };

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                // Optional: log error
                Console.WriteLine("Email sending error: " + ex.Message);
            }
        }

    }
}
