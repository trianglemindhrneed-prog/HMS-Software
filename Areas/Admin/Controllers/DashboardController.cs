using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using HMSCore.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;


namespace HMSCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : BaseController
    {
        private readonly IDbLayer _dbLayer;

        public DashboardController(IDbLayer dbLayer)
        {
            _dbLayer = dbLayer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();

            // 🔥 Single SP call for dashboard data
            var dtDashboard = await _dbLayer.ExecuteSPAsync(
                "sp_GetDashboardData",
                new[] { new SqlParameter("@Action", "GetDashboardStats") }
            );

            if (dtDashboard.Rows.Count > 0)
            {
                var row = dtDashboard.Rows[0];

                // OPD Stats
                model.OPDTodaysPatients = row["TodaysPatients"] != DBNull.Value ? (int)row["TodaysPatients"] : 0;
                model.OPDNewPatients = row["OPDNewPatients"] != DBNull.Value ? (int)row["OPDNewPatients"] : 0;
                model.OPDTotalPatients = row["OPDTotalPatients"] != DBNull.Value ? (int)row["OPDTotalPatients"] : 0;
                model.TodaysAppointments = row["TodaysAppointments"] != DBNull.Value ? (int)row["TodaysAppointments"] : 0;
                model.TotalPatientsSeen = row["TotalPatientsSeen"] != DBNull.Value ? (int)row["TotalPatientsSeen"] : 0;
                model.TotalPaidAmount = row["TotalPaidAmount"] != DBNull.Value ? Convert.ToDecimal(row["TotalPaidAmount"]) : 0;
                model.TotalDueAmount = row["TotalDueAmount"] != DBNull.Value ? Convert.ToDecimal(row["TotalDueAmount"]) : 0;
                model.TotalSellMedicine = row["TotalSellMedicine"] != DBNull.Value ? Convert.ToDecimal(row["TotalSellMedicine"]) : 0;
                model.TotalDoctors = row["TotalDoctors"] != DBNull.Value ? (int)row["TotalDoctors"] : 0;
                model.TotalNurses = row["TotalNurses"] != DBNull.Value ? (int)row["TotalNurses"] : 0;

                // IPD Stats
                model.IPDNewPatients = row["IPDNewPatients"] != DBNull.Value ? (int)row["IPDNewPatients"] : 0;
                model.IPDTotalPatients = row["IPDTotalPatients"] != DBNull.Value ? (int)row["IPDTotalPatients"] : 0;
                model.TotalIPDSellMedicine = row["TotalIPDSellMedicine"] != DBNull.Value ? Convert.ToDecimal(row["TotalIPDSellMedicine"]) : 0;
                model.AvailableBeds = row["AvailableBeds"] != DBNull.Value ? (int)row["AvailableBeds"] : 0;
                model.OccupiedBeds = row["OccupiedBeds"] != DBNull.Value ? (int)row["OccupiedBeds"] : 0;

                // Lab/Pathology Stats
                model.LabCategoryCount = row["LabCategoryCount"] != DBNull.Value ? (int)row["LabCategoryCount"] : 0;
                model.LabTestCount = row["LabTestCount"] != DBNull.Value ? (int)row["LabTestCount"] : 0;
                model.PendingTests = row["PendingTests"] != DBNull.Value ? (int)row["PendingTests"] : 0;
                model.DeliveredTests = row["DeliveredTests"] != DBNull.Value ? (int)row["DeliveredTests"] : 0;
            }

            // User role from session/ViewBag
            model.UserRole = ViewBag.UserRole as string;

            return View(model);
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int userId = Convert.ToInt32(HttpContext.Session.GetInt32("UserId"));
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var resultParam = new SqlParameter("@Result", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _dbLayer.ExecuteSPAsync(
                "sp_ChangePassword",
                new[]
                {
            new SqlParameter("@UserId", userId),
            new SqlParameter("@CurrentPassword", model.CurrentPassword),
            new SqlParameter("@NewPassword", model.NewPassword),
            resultParam
                }
            );

            int result = Convert.ToInt32(resultParam.Value);

            if (result == -1)
            {
                TempData["Message"] = "Current password is incorrect.";
                TempData["MessageType"] = "error";
                
                return View(model);
            }
            else if (result == 1)
            {
                TempData["Message"] = "Password changed successfully.";
                TempData["MessageType"] = "success"; 
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Message"] = "Server error. Please try again.";
                TempData["MessageType"] = "error"; 
                return View(model);
            }
        }




    }
}
