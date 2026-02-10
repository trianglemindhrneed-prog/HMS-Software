using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult PatientReportMis()
        {
            return View();
        }
        public IActionResult PaidAmountReport()
        {
            return View();
        }
        public IActionResult DueAmountReport()
        {
            return View();
        }
        public IActionResult BillSaleProductMIS()
        {
            return View();
        }
        public IActionResult PatientAppointmentMIS()
        {
            return View();
        }
        

    }
}
