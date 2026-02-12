using HMSCore.Areas.Admin.Models;
using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;

namespace HMSCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OpdMisController : BaseController
    {
        private readonly IDbLayer _dbLayer;
        private readonly IConfiguration _configuration;
        public OpdMisController(IDbLayer dbLayer, IConfiguration configuration)
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
            SqlParameter[] summaryParams = new SqlParameter[]
             {
              new SqlParameter("@Action", "GetPatientSummary")
              };

            DataTable dtSummary = await _dbLayer.ExecuteSPAsync("sp_OpdManagePatients", summaryParams);

            int yearlyCount = 0;
            int halfYearlyCount = 0;
            int monthlyCount = 0;

            if (dtSummary.Rows.Count > 0)
            {
                yearlyCount = dtSummary.Rows[0]["Yearly"] == DBNull.Value ? 0 : Convert.ToInt32(dtSummary.Rows[0]["Yearly"]);
                halfYearlyCount = dtSummary.Rows[0]["HalfYearly"] == DBNull.Value ? 0 : Convert.ToInt32(dtSummary.Rows[0]["HalfYearly"]);
                monthlyCount = dtSummary.Rows[0]["Monthly"] == DBNull.Value ? 0 : Convert.ToInt32(dtSummary.Rows[0]["Monthly"]);
            }
            var vm = new PatientsDetailsViewModel
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
        public async Task<IActionResult> DeleteOPDPatients(int id)
        {
            await _dbLayer.ExecuteSPAsync("sp_OpdManagePatients", new[]
            {
        new SqlParameter("@Action", "DeletePatient"),
        new SqlParameter("@PatientId", id)
    });

            TempData["Message"] = "Patient deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("PatientReportMis");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOPDPatientsSelected(int[] selectedIds)
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

            return RedirectToAction("PatientReportMis");
        }

        [HttpGet]
        public async Task<IActionResult> PaidAmountReport(
         string filterColumn = null,
         string keyword = null,
         string fromDate = null,
         string toDate = null,
         int pageSize = 20)
        {
            // --- Parse dates ---
            object fromDateValue = string.IsNullOrEmpty(fromDate)
                ? DBNull.Value
                : DateTime.ParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            object toDateValue = string.IsNullOrEmpty(toDate)
                ? DBNull.Value
                : DateTime.ParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(1);

            // --- Get Patients ---
            SqlParameter[] patientParams = new SqlParameter[]
            {
                new SqlParameter("@Action", "SelectPatients"),
                new SqlParameter("@FilterColumn", string.IsNullOrEmpty(filterColumn) ? DBNull.Value : (object)filterColumn),
                new SqlParameter("@Keyword", string.IsNullOrEmpty(keyword) ? DBNull.Value : (object)keyword),
                new SqlParameter("@FromDate", fromDateValue),
                new SqlParameter("@ToDate", toDateValue)
            };

            DataTable dtPatients = await _dbLayer.ExecuteSPAsync("sp_PaidAmountReport", patientParams);

            var patients = dtPatients.AsEnumerable().Select(r => new PaidPatientDetails
            {
                BillId = r["BillId"] == DBNull.Value ? 0 : Convert.ToInt32(r["BillId"]),
                BillNo = r["BillNo"].ToString(),
                PatientId = r["PatientNo"].ToString(),
                PatientName = r["Name"].ToString(),
                ConsultFee = r["ConsultFee"].ToString(),
                TotalAmount = r["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TotalAmount"]),
                Discount = r["Discount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Discount"]),
                DiscountAmount = r["TotalAmount"] == DBNull.Value || r["Discount"] == DBNull.Value
          ? 0
          : Convert.ToDecimal(r["TotalAmount"]) * Convert.ToDecimal(r["Discount"]) / 100,
                GrandTotal = r["GrandTotal"] == DBNull.Value ? 0 : Convert.ToDecimal(r["GrandTotal"]),
                PaidAmount = r["PaidValue"] == DBNull.Value ? 0 : Convert.ToDecimal(r["PaidValue"]),

                // THIS IS THE FIX
                Balance = (r["GrandTotal"] == DBNull.Value ? 0 : Convert.ToDecimal(r["GrandTotal"]))
              - (r["PaidValue"] == DBNull.Value ? 0 : Convert.ToDecimal(r["PaidValue"])),

                BillDate = r["BillDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["BillDate"]),
                Status = (r["GrandTotal"] == DBNull.Value ? 0 : Convert.ToDecimal(r["GrandTotal"]))
               - (r["PaidValue"] == DBNull.Value ? 0 : Convert.ToDecimal(r["PaidValue"])) == 0
               ? "Complete"
               : "Pending"
            }).ToList();


            // --- Get Paid Summary ---
            SqlParameter[] summaryParams = new SqlParameter[]
            {
                new SqlParameter("@Action", "GetSummary")
            };

            DataTable dtSummary = await _dbLayer.ExecuteSPAsync("sp_PaidAmountReport", summaryParams);

            decimal yearlyPaid = 0, halfYearlyPaid = 0, monthlyPaid = 0;
            if (dtSummary.Rows.Count > 0)
            {
                yearlyPaid = dtSummary.Rows[0]["YearlyPaid"] == DBNull.Value ? 0 : Convert.ToDecimal(dtSummary.Rows[0]["YearlyPaid"]);
                halfYearlyPaid = dtSummary.Rows[0]["HalfYearlyPaid"] == DBNull.Value ? 0 : Convert.ToDecimal(dtSummary.Rows[0]["HalfYearlyPaid"]);
                monthlyPaid = dtSummary.Rows[0]["MonthlyPaid"] == DBNull.Value ? 0 : Convert.ToDecimal(dtSummary.Rows[0]["MonthlyPaid"]);
            }

            // --- Build ViewModel ---
            var vm = new PaidAmountReportViewModel
            {
                PageSize = pageSize,
                FilterColumn = filterColumn,
                Keyword = keyword,
                FromDate = string.IsNullOrEmpty(fromDate) ? (DateTime?)null : DateTime.Parse(fromDate),
                ToDate = string.IsNullOrEmpty(toDate) ? (DateTime?)null : DateTime.Parse(toDate),
                Patients = patients,
                YearlyPaid = yearlyPaid,
                HalfYearlyPaid = halfYearlyPaid,
                MonthlyPaid = monthlyPaid
            };

            return View(vm);
        }


        [HttpGet]
        public async Task<IActionResult> DueAmountReport(
        string filterColumn = null,
        string keyword = null,
        string fromDate = null,
        string toDate = null,
        int pageSize = 20)
        {
            // --- Parse dates ---
            object fromDateValue = string.IsNullOrEmpty(fromDate)
                ? DBNull.Value
                : DateTime.ParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            object toDateValue = string.IsNullOrEmpty(toDate)
                ? DBNull.Value
                : DateTime.ParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(1);

            // --- Get Patients ---
            SqlParameter[] patientParams = new SqlParameter[]
            {
        new SqlParameter("@Action", "SelectPatients"),
        new SqlParameter("@FilterColumn", string.IsNullOrEmpty(filterColumn) ? DBNull.Value : (object)filterColumn),
        new SqlParameter("@Keyword", string.IsNullOrEmpty(keyword) ? DBNull.Value : (object)keyword),
        new SqlParameter("@FromDate", fromDateValue),
        new SqlParameter("@ToDate", toDateValue)
            };

            DataTable dtPatients = await _dbLayer.ExecuteSPAsync("sp_PaidAmountReport", patientParams);

            var patients = dtPatients.AsEnumerable().Select(r => new PaidPatientDetails
            {
                BillId = r["BillId"] == DBNull.Value ? 0 : Convert.ToInt32(r["BillId"]),
                BillNo = r["BillNo"].ToString(),
                PatientId = r["PatientNo"].ToString(),
                PatientName = r["Name"].ToString(),
                ConsultFee = r["ConsultFee"].ToString(),
                TotalAmount = r["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TotalAmount"]),
                Discount = r["Discount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Discount"]),
                DiscountAmount = r["TotalAmount"] == DBNull.Value || r["Discount"] == DBNull.Value
                    ? 0
                    : Convert.ToDecimal(r["TotalAmount"]) * Convert.ToDecimal(r["Discount"]) / 100,
                GrandTotal = r["GrandTotal"] == DBNull.Value ? 0 : Convert.ToDecimal(r["GrandTotal"]),
                PaidAmount = r["PaidValue"] == DBNull.Value ? 0 : Convert.ToDecimal(r["PaidValue"]),
                Balance = r["Balance"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Balance"]),
                BillDate = r["BillDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["BillDate"]),
                Status = r["Balance"] == DBNull.Value || Convert.ToDecimal(r["Balance"]) == 0 ? "Complete" : "Pending"
            }).ToList();

            // --- Get Due Summary ---
            SqlParameter[] dueSummaryParams = new SqlParameter[]
            {
        new SqlParameter("@Action", "GetDueSummary")
            };

            DataTable dtDueSummary = await _dbLayer.ExecuteSPAsync("sp_PaidAmountReport", dueSummaryParams);

            decimal yearlyDue = 0, halfYearlyDue = 0, monthlyDue = 0;
            if (dtDueSummary.Rows.Count > 0)
            {
                yearlyDue = dtDueSummary.Rows[0]["YearlyDue"] == DBNull.Value ? 0 : Convert.ToDecimal(dtDueSummary.Rows[0]["YearlyDue"]);
                halfYearlyDue = dtDueSummary.Rows[0]["HalfYearlyDue"] == DBNull.Value ? 0 : Convert.ToDecimal(dtDueSummary.Rows[0]["HalfYearlyDue"]);
                monthlyDue = dtDueSummary.Rows[0]["MonthlyDue"] == DBNull.Value ? 0 : Convert.ToDecimal(dtDueSummary.Rows[0]["MonthlyDue"]);
            }

            // --- Build ViewModel ---
            var vm = new PaidAmountReportViewModel
            {
                PageSize = pageSize,
                FilterColumn = filterColumn,
                Keyword = keyword,
                FromDate = string.IsNullOrEmpty(fromDate) ? (DateTime?)null : DateTime.Parse(fromDate),
                ToDate = string.IsNullOrEmpty(toDate) ? (DateTime?)null : DateTime.Parse(toDate),
                Patients = patients,
                YearlyPaid = 0,            // leave 0 or fill from Paid summary if needed
                HalfYearlyPaid = 0,
                MonthlyPaid = 0,
                YearlyDue = yearlyDue,
                HalfYearlyDue = halfYearlyDue,
                MonthlyDue = monthlyDue
            };

            return View(vm);
        }


        [HttpGet]
        public async Task<IActionResult> BillSaleProductMIS(
            string filterColumn = null,
            string keyword = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            // 1️⃣ Get the list of invoices from SP using SELECT action
            var dt = await _dbLayer.ExecuteSPAsync(
                "SP_SaleMedicineMisReport",
                new[]
                {
            new SqlParameter("@Action", "SELECT"),
            new SqlParameter("@FilterColumn", (object)filterColumn ?? DBNull.Value),
            new SqlParameter("@Keyword", (object)keyword ?? DBNull.Value),
            new SqlParameter("@FromDate", (object)fromDate ?? DBNull.Value),
            new SqlParameter("@ToDate", (object)toDate?.AddDays(1) ?? DBNull.Value)
                });

            var list = dt.AsEnumerable().Select(r => new SaleMedicineReportViewModel
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
                "SP_SaleMedicineMisReport",
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
            return View(new SaleMedicineReportPageVM
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
        public async Task<IActionResult> DeleteMedicineBillMis(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Message"] = "Invalid invoice";
                TempData["MessageType"] = "error";
                return RedirectToAction("BillSaleProductMIS");
            }

            await _dbLayer.ExecuteSPAsync(
                "SP_SaleMedicineMisReport",
                new[]
                {
            new SqlParameter("@Action", "DELETE"),
            new SqlParameter("@InvoiceId", id)
                });

            TempData["Message"] = "Invoice deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("BillSaleProductMIS");
        }


        [HttpPost]
        public async Task<IActionResult> DeleteSelectedMisMedicine(string[] selectedIds)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData["Message"] = "No invoice selected";
                TempData["MessageType"] = "error";
                return RedirectToAction("BillSaleProductMIS");
            }

            foreach (var invoiceId in selectedIds)
            {
                await _dbLayer.ExecuteSPAsync(
                    "SP_SaleMedicineMisReport",
                    new[]
                    {
                new SqlParameter("@Action", "DELETE"),
                new SqlParameter("@InvoiceId", invoiceId)
                    });
            }

            TempData["Message"] = "Selected invoices deleted successfully";
            TempData["MessageType"] = "success";

            return RedirectToAction("BillSaleProductMIS");
        }
       
        [HttpGet]
        public async Task<IActionResult> PatientAppointmentMIS(string filterColumn = null, string keyword = null)
        {
            // 1. Get the list of doctors and patients seen
            var dt = await _dbLayer.ExecuteSPAsync(
                "SP_DoctorPatientSeenSummary",
                new[]
                {
                new SqlParameter("@Action", "SELECT"),
                new SqlParameter("@FilterColumn", (object)filterColumn ?? DBNull.Value),
                new SqlParameter("@Keyword", (object)keyword ?? DBNull.Value)
                });

            var list = dt.AsEnumerable().Select(r => new DoctorPatientSeenReportVM
            {
                DoctorName = r["DoctorName"]?.ToString(),
                MobileNu = r["MobileNu"]?.ToString(),
                Email = r["Email"]?.ToString(),
                PatientSeenCount = r["PatientSeenCount"] != DBNull.Value ? Convert.ToInt32(r["PatientSeenCount"]) : 0
            }).ToList();

            // 2. Get summary
            var dtSummary = await _dbLayer.ExecuteSPAsync(
                "SP_DoctorPatientSeenSummary",
                new[] { new SqlParameter("@Action", "SUMMARY") }
            );

            int totalDoctors = 0, totalPatientsSeen = 0;

            if (dtSummary.Rows.Count > 0)
            {
                var row = dtSummary.Rows[0];
                totalDoctors = row["TotalDoctors"] != DBNull.Value ? Convert.ToInt32(row["TotalDoctors"]) : 0;
                totalPatientsSeen = row["TotalPatientsSeen"] != DBNull.Value ? Convert.ToInt32(row["TotalPatientsSeen"]) : 0;
            }

            return View(new DoctorPatientSeenPageVM
            {
                FilterColumn = filterColumn,
                Keyword = keyword,
                Records = list,
                TotalDoctors = totalDoctors,
                TotalPatientsSeen = totalPatientsSeen
            });
        }

    }
}
