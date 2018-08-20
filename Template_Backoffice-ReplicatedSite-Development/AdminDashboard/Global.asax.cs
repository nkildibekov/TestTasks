using AdminDashboard.Models;
using Common;
using Common.Helpers;
using Common.Providers;
using Common.Services;
using ExigoService;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.WebPages;


namespace AdminDashboard
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static DateTime ApplicationStartDate;



        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Set the application's start date for easy reference
            ApplicationStartDate = DateTime.Now;
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            try
            {
                if (GlobalSettings.ErrorLogging.ErrorLoggingEnabled && !Request.IsLocal)
                {
                    ErrorLogger.LogException(Server.GetLastError(), Request.RawUrl);
                }
            }
            catch { }
        }

        public override void Init()
        {
            this.PostAuthenticateRequest += new EventHandler(MvcApplication_PostAuthenticateRequest);
            base.Init();
        }

        void MvcApplication_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                var identity = UserIdentity.Deserialize(authCookie.Value);
                if (identity == null)
                {
                    FormsAuthentication.SignOut();
                }
                else
                {
                    HttpContext.Current.User = new GenericPrincipal(identity, null);
                    Context.User = new GenericPrincipal(identity, null);
                }
            }
        }
    }
}