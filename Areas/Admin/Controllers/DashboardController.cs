using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace HMSCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly IDbLayer _dbLayer;

        public DashboardController(IDbLayer dbLayer)
        {
            _dbLayer = dbLayer;
        }

        public async Task<IActionResult> Index()
        { 
            var userId = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var userType = HttpContext.Session.GetString("UserType"); 
             
            if (userId == null || string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }
             
            ViewBag.UserName = userName;
            ViewBag.UserRole = userType; 

            return View();
        }
    }
}
