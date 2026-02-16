using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;

namespace HMSCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class IpdMisController : BaseController
    {
        private readonly IDbLayer _dbLayer;
        private readonly IConfiguration _configuration;
        public IpdMisController(IDbLayer dbLayer, IConfiguration configuration)
        {
            _dbLayer = dbLayer;
            _configuration = configuration;
        }


        [HttpGet]
        public async Task<IActionResult> PatientReportMis(
      string filterColumn = null,
      string keyword = null,
      string fromDate = null,
      string toDate = null,
      int pageSize = 20)
        {
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Account", new { area = "" });

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
            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_IpdManagePatientsMis", parameters);

            // Map DataTable to ViewModel with DBNull-safe conversions
            var patients = dt.AsEnumerable().Select(r => new IpdAdmission
            {
                AdmissionID = r["AdmissionID"] == DBNull.Value ? 0 : Convert.ToInt32(r["AdmissionID"]),
                PatientID = r["PatientId"] == DBNull.Value ? null : r["PatientId"].ToString(),
                Name = r["PatientName"] == DBNull.Value ? null : r["PatientName"].ToString(),
                Age = r["Age"] == DBNull.Value ? 0 : Convert.ToInt32(r["Age"]),
                Number = r["Number"] == DBNull.Value ? null : r["Number"].ToString(), 
                DoctorName = r["DoctorName"] == DBNull.Value ? null : r["DoctorName"].ToString(),
                DoctorNumber = r["DoctorNumber"] == DBNull.Value ? null : r["DoctorNumber"].ToString(),
                AdmissionDateTime = r["AdmissionDateTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["AdmissionDateTime"])
            }).ToList();
            SqlParameter[] summaryParams = new SqlParameter[]
             {
              new SqlParameter("@Action", "GetPatientSummary")
              };

            DataTable dtSummary = await _dbLayer.ExecuteSPAsync("sp_IpdManagePatientsMis", summaryParams);

            int yearlyCount = 0;
            int halfYearlyCount = 0;
            int monthlyCount = 0;

            if (dtSummary.Rows.Count > 0)
            {
                yearlyCount = dtSummary.Rows[0]["Yearly"] == DBNull.Value ? 0 : Convert.ToInt32(dtSummary.Rows[0]["Yearly"]);
                halfYearlyCount = dtSummary.Rows[0]["HalfYearly"] == DBNull.Value ? 0 : Convert.ToInt32(dtSummary.Rows[0]["HalfYearly"]);
                monthlyCount = dtSummary.Rows[0]["Monthly"] == DBNull.Value ? 0 : Convert.ToInt32(dtSummary.Rows[0]["Monthly"]);
            }
            var vm = new IpdAdmission
            {
                PageSize = pageSize,
                FilterColumn = filterColumn,
                Keyword = keyword,
                FromDate = string.IsNullOrEmpty(fromDate) ? (DateTime?)null : DateTime.Parse(fromDate),
                ToDate = string.IsNullOrEmpty(toDate) ? (DateTime?)null : DateTime.Parse(toDate),
                Patients = patients,

                Yearly = yearlyCount,
                HalfYearly = halfYearlyCount,
                Monthly = monthlyCount
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteIPdPatients(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_IpdManagePatientsMis", new[]
            {
        new SqlParameter("@Action", "DeletePatient"),
        new SqlParameter("@PatientId", id)
    });

            TempData["Message"] = "Patient deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("PatientReportMis");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteIPDPatientsSelected(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                foreach (var id in selectedIds)
                {
                    await _dbLayer.ExecuteSPAsync("sp_IpdManagePatientsMis", new[]
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

            return RedirectToAction("PatientReportMis");
        }

        [HttpGet]
        public async Task<IActionResult> IpdBillSaleProductMIS(
     string filterColumn = null,
     string keyword = null,
     DateTime? fromDate = null,
     DateTime? toDate = null)
        {
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
                return RedirectToAction("Login", "Account", new { area = "" });
            // 1️⃣ Get the list of invoices from SP using SELECT action
            var dt = await _dbLayer.ExecuteSPAsync(
                "SP_SaleMedicineIpdMisReport",
                new[]
                {
            new SqlParameter("@Action", "SELECT"),
            new SqlParameter("@FilterColumn", (object)filterColumn ?? DBNull.Value),
            new SqlParameter("@Keyword", (object)keyword ?? DBNull.Value),
            new SqlParameter("@FromDate", (object)fromDate ?? DBNull.Value),
            new SqlParameter("@ToDate", (object)toDate?.AddDays(1) ?? DBNull.Value)
                });

            var list = dt.AsEnumerable().Select(r => new IPDSaleMedicineReportViewModel
            {
                Id = r["Id"] != DBNull.Value ? Convert.ToInt32(r["Id"]) : 0,
                InvoiceId = r["InvoiceId"]?.ToString(),
                InvoiceDate = r["InvoiceDate"] != DBNull.Value ? Convert.ToDateTime(r["InvoiceDate"]) : DateTime.MinValue,
                PatientId = r["PatientId"]?.ToString(),
                PatientName = r["PatientName"]?.ToString(),
                GrandTotal = r["GrandTotal"] != DBNull.Value ? Convert.ToDecimal(r["GrandTotal"]) : 0,
                FinalAmount = r["FinalAmount"] != DBNull.Value ? Convert.ToDecimal(r["FinalAmount"]) : 0
            }).ToList();

            // 2️⃣ Get summary totals (Yearly/Half-Yearly/Monthly) from SP
            var dtSummary = await _dbLayer.ExecuteSPAsync(
                "SP_SaleMedicineIpdMisReport",
                new[] { new SqlParameter("@Action", "SUMMARY") }  // SUMMARY action in SP
            );

            decimal yearlyPaid = 0, halfYearlyPaid = 0, monthlyPaid = 0;

            if (dtSummary.Rows.Count > 0)
            {
                var row = dtSummary.Rows[0];
                yearlyPaid = row["YearlyDue"] != DBNull.Value ? Convert.ToDecimal(row["YearlyDue"]) : 0;
                halfYearlyPaid = row["HalfYearlyDue"] != DBNull.Value ? Convert.ToDecimal(row["HalfYearlyDue"]) : 0;
                monthlyPaid = row["MonthlyDue"] != DBNull.Value ? Convert.ToDecimal(row["MonthlyDue"]) : 0;
            }

            // 3️⃣ Return the view with both invoice list and summary
            return View(new IPDSaleMedicineReportPageVM
            {
                FilterColumn = filterColumn,
                Keyword = keyword,
                FromDate = fromDate,
                ToDate = toDate,
                Records = list,
                YearlyPaid = yearlyPaid,
                HalfYearlyPaid = halfYearlyPaid,
                MonthlyPaid = monthlyPaid
            });
        }



        [HttpPost]
        public async Task<IActionResult> DeleteMedicineBillIpdMis(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Message"] = "Invalid invoice";
                TempData["MessageType"] = "error";
                return RedirectToAction("IpdBillSaleProductMIS");
            }

            await _dbLayer.ExecuteSPAsync(
                "SP_SaleMedicineIpdMisReport",
                new[]
                {
            new SqlParameter("@Action", "DELETE"),
            new SqlParameter("@InvoiceId", id)
                });

            TempData["Message"] = "Invoice deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("IpdBillSaleProductMIS");
        }


        [HttpPost]
        public async Task<IActionResult> DeleteSelectedIpdMisMedicine(string[] selectedIds)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData["Message"] = "No invoice selected";
                TempData["MessageType"] = "error";
                return RedirectToAction("IpdBillSaleProductMIS");
            }

            foreach (var invoiceId in selectedIds)
            {
                await _dbLayer.ExecuteSPAsync(
                    "SP_SaleMedicineIpdMisReport",
                    new[]
                    {
                new SqlParameter("@Action", "DELETE"),
                new SqlParameter("@InvoiceId", invoiceId)
                    });
            }

            TempData["Message"] = "Selected invoices deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("IpdBillSaleProductMIS");
        }






    }
}
