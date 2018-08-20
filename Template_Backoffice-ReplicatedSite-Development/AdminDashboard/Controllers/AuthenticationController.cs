using AdminDashboard.Services;
using AdminDashboard.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace AdminDashboard.Controllers
{
    [AllowAnonymous]
    public class AuthenticationController : Controller
    {
        #region Signing in
        [Route("~/login")]
        public ActionResult Login()
        {
            if (Request.IsAuthenticated)
            {
                return RedirectToRoute(new
                {
                    controller = "app"
                });
            }

            var model = new LoginViewModel();

            return View(model);
        }

        [HttpPost]
        [Route("~/login")]
        public JsonNetResult Login(LoginViewModel model)
        {
            var service = new IdentityService();
            var response = service.SignIn(model.LoginName, model.Password);

            return new JsonNetResult(response);
        }
        #endregion

        #region Signing Out
        [Route("~/logout")]
        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
        #endregion  
    }
}
