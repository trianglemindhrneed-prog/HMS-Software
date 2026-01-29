using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HMSCore.Areas.Admin.Controllers
{
    public class BaseController : Controller
    {

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string userRole = HttpContext.Session.GetString("UserType");
            ViewBag.UserRole = userRole;

            base.OnActionExecuting(filterContext);
        }

    }
}
