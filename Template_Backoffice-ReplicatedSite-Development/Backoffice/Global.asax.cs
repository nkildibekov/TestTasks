using Common;
using Common.Helpers;
using System;
using System.Globalization;
using System.Security.Principal;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.WebPages;
using ExigoService;
using Serilog;
using SerilogWeb.Classic;
using Serilog.Events;
using Common.Services;
using Caching;
using System.Configuration;

namespace Backoffice
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
            DisplayConfig.RegisterDisplayModes(DisplayModeProvider.Instance.Modes);
            ModelBinderConfig.RegisterModelBinders(ModelBinders.Binders);

            // Set the application's start date for easy reference
            ApplicationStartDate = DateTime.Now;


            #region Serilog 
            // ** NOTE: DO NOT ENABLE UNTIL YOU CHANGE THE COMPANY BELOW..
            //  EXAMPLE: .Enrich.WithProperty("App", "Company.Backoffice") 
            //  should change to...
            //  .Enrich.WithProperty("App", "ExigoDemo.Backoffice") 
            // **


            /* SeriLog */
            //var version = this.GetType().Assembly.GetName().Version;

            //ApplicationLifecycleModule.RequestLoggingLevel = LogEventLevel.Verbose;
            //ApplicationLifecycleModule.LogPostedFormData = LogPostedFormDataOption.OnlyOnError;
            //ApplicationLifecycleModule.FormDataLoggingLevel = LogEventLevel.Error;

            //Log.Logger = new LoggerConfiguration()
            //    .Enrich.WithMachineName()
            //    .Enrich.WithProperty("App", "Company.Backoffice")
            //     .Enrich.WithProperty("Version", $"{version.Major}.{version.Minor}.{version.Build}")
            //    .WriteTo.Seq("https://log.exigo.com")
            //    .CreateLogger();

            //Log.Information("Starting up web");
            #endregion

            // SEE CODE ABOVE IN Application_Start FIRST
            //protected void Application_End()
            //{
            //    Log.CloseAndFlush();
            //}

            #region UseDbOrRedisforSessionData
            //First we determine if we are going to use Redis (client must have purchased redis in azure) or SQL in memory caching.
            //If SQL than we fire off operation that will check the sql db to ensure that everything is set up to run in memory caching.
            var sessionCacheProvider = (GlobalSettings.Exigo.UserSession.UseDbSessionCaching)
                ? new SqlInMemoryCacheProvider(GlobalSettings.Exigo.Api.Sql.ConnectionStrings.SqlReporting)
                : new RedisCacheProvider(GlobalSettings.Exigo.Caching.RedisConnectionString) as ICacheProvider;

            CacheConfig.RegisterCache(sessionCacheProvider);
            #endregion

            #region Force Report Cache to Refresh
            if (GlobalSettings.Backoffices.Reports.CommissionReportCacheRefresh.UseTaskToForceReportCacheRefresh)
            {
                Common.Services.CommissionReportRefreshTaskService.InitializeCommissionReportRefreshTask();
            }
            #endregion

        }

        public override void Init()
        {
            this.BeginRequest += new EventHandler(Application_BeginRequest);
            this.PostAuthenticateRequest += new EventHandler(MvcApplication_PostAuthenticateRequest);
            base.Init();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            GlobalSettings.Exigo.VerifyEnvironment(HttpContext.Current);
            //if (Request.IsSecureConnection)
            //{
            //    Response.AddHeader("Strict-Transport-Security", "max-age=31536000");
            //}
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

        void MvcApplication_PostAuthenticateRequest(object sender, EventArgs e)
        {
            // Set the language
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(Exigo.GetSelectedLanguage());

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
                    var principal = new GenericPrincipal(identity, null);
                    HttpContext.Current.User = principal;
                    System.Threading.Thread.CurrentPrincipal = principal;
                    Context.User = new GenericPrincipal(identity, null);


                    // Set the culture
                    GlobalUtilities.SetCurrentCulture(Identity.Current.Market.CultureCode);

                }
            }
        }
    }
}
