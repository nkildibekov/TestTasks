using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AdminDashboard.Controllers
{
    public class ErrorController : Controller
    {
        public ActionResult Index()
        {
            return UnexpectedError();
        }

        public ActionResult UnexpectedError()
        {
            return View();
        }
        public ActionResult NotFound()
        {
            return View();
        }
    }
}
