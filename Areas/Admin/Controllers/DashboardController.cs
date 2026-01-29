using HMSCore.Data;
using Microsoft.AspNetCore.Mvc;
using System.Data;

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
        public IActionResult Index()
        {  
            return View();
        }

       


    }
}
