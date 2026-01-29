using HMSCore.Data;
using HMSCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HMSCore.Controllers
{
    public class AccountController : Controller
    {
        private readonly IDbLayer _dbLayer;

        public AccountController(IDbLayer dbLayer)
        {
            _dbLayer = dbLayer;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost] 
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@UserName", model.Username),
        new SqlParameter("@Password", model.Password)
            };

            DataTable dt = await _dbLayer.ExecuteSPAsync("sp_LoginUser", parameters);

            if (dt.Rows.Count == 0)
            {
                ViewData["ErrorMessage"] = "Invalid username or password.";
                return View(model);
            }

            DataRow row = dt.Rows[0];

            if (Convert.ToInt32(row["LoginSuccess"]) == 0)
            {
                ViewData["ErrorMessage"] = row["Message"].ToString();
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", Convert.ToInt32(row["UserId"]));
            HttpContext.Session.SetString("UserName", row["UserName"].ToString());
            HttpContext.Session.SetString("UserType", row["UserType"].ToString());

            if (row["DoctorId"] != DBNull.Value)
                HttpContext.Session.SetInt32("DoctorId", Convert.ToInt32(row["DoctorId"]));

            if (row["NurseId"] != DBNull.Value)
                HttpContext.Session.SetInt32("NurseId", Convert.ToInt32(row["NurseId"]));

            // Redirect to Admin Dashboard
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        
            [HttpPost]
            public IActionResult Logout()
            { 
                HttpContext.Session.Clear(); 
                // await HttpContext.SignOutAsync();

                return RedirectToAction("Login", "Account");
            }
       
   

}
}
