﻿using Common;
using Common.Helpers;
using ReplicatedSite.Services;
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
using ExigoService;
using Serilog;
using SerilogWeb.Classic;
using Serilog.Events;
using Common.Services;
using Caching;

namespace ReplicatedSite
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static DateTime ApplicationStartDate;

        public override void Init()
        {
            this.BeginRequest += new EventHandler(Application_BeginRequest);
            this.PostAuthenticateRequest += new EventHandler(MvcApplication_PostAuthenticateRequest);

            base.Init();
        }

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
            //    .Enrich.WithProperty("App", "ExigoDemo.ReplicatedSite")
            //     .Enrich.WithProperty("Version", $"{version.Major}.{version.Minor}.{version.Build}")
            //    .WriteTo.Seq("https://log.exigo.com")
            //    .CreateLogger();

            //Log.Information("Starting up web");
            #endregion

            #region UseDbOrRedisforSessionData
            //First we determine if we are going to use Redis (client must have purchased redis in azure) or SQL in memory caching.
            //If SQL than we fire off operation that will check the sql db to ensure that everything is set up to run in memory caching.
            var sessionCacheProvider = (GlobalSettings.Exigo.UserSession.UseDbSessionCaching)
                ? new SqlInMemoryCacheProvider(GlobalSettings.Exigo.Api.Sql.ConnectionStrings.SqlReporting)
                : new RedisCacheProvider(GlobalSettings.Exigo.Caching.RedisConnectionString) as ICacheProvider;

            CacheConfig.RegisterCache(sessionCacheProvider);
            #endregion
        }

        // SEE CODE ABOVE IN Application_Start FIRST
        //protected void Application_End()
        //{
        //    Log.CloseAndFlush();
        //}

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            GlobalSettings.Exigo.VerifyEnvironment(HttpContext.Current);
            if (Request.IsSecureConnection)
            {
                Response.AddHeader("Strict-Transport-Security", "max-age=31536000");
            }

            // Get the route data
            var routeData = RouteTable.Routes.GetRouteData(new HttpContextWrapper(HttpContext.Current));

            // Account for attribute routing and null routeData
            if (routeData != null && routeData.Values.ContainsKey("MS_DirectRouteMatches"))
            {
                routeData = ((List<RouteData>)routeData.Values["MS_DirectRouteMatches"]).First();
            }


            // If we have an identity and the current identity matches the web alias in the routes, stop here.
            var identity = HttpContext.Current.Items["OwnerWebIdentity"] as ReplicatedSiteIdentity;
            if (routeData == null
                || routeData.Values["webalias"] == null
                || (identity != null && identity.WebAlias.Equals(routeData.Values["webalias"].ToString(), StringComparison.InvariantCultureIgnoreCase)))
            {
                return;
            }


            // Determine some web alias data
            var urlHelper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), RouteTable.Routes.GetRouteData(new HttpContextWrapper(HttpContext.Current))));
            var currentWebAlias = routeData.Values["webalias"].ToString();
            var defaultWebAlias = GlobalSettings.ReplicatedSites.DefaultWebAlias;
            var lastWebAlias = GlobalUtilities.GetLastWebAlias(defaultWebAlias);
            var defaultPage = urlHelper.Action(routeData.Values["action"].ToString(), routeData.Values["controller"].ToString(), new { webalias = lastWebAlias });


            // This ensures that if the page is redirected because of web alias switching, that athe querystring params are passed as well
            if (currentWebAlias.ToLower() == GlobalSettings.ReplicatedSites.DefaultWebAlias.ToLower())
            {
                // Create new route value dictionary
                var newList = new RouteValueDictionary();

                // Pull in all values that are not the controller,action or webalias
                foreach (var routeValue in routeData.Values.Where(c => c.Key != "action" && c.Key != "controller" && c.Key != "webalias"))
                {
                    // Add all values that arent empty to the route data.
                    if (routeValue.Value.ToString().IsNotNullOrEmpty())
                    {
                        newList.Add(routeValue.Key, routeValue.Value);
                    }
                }
                // Grab query in case there are any pieces being sent in with ?example=value
                var query = Request.Url.Query;

                //add webalias to the route values.
                newList.Add("webalias", lastWebAlias);

                // create new url using new route values and add the query at the end.
                defaultPage = urlHelper.Action(routeData.Values["action"].ToString(), routeData.Values["controller"].ToString(), newList) + query;
            }


            // If we are an orphan and we don't allow them, redirect to a capture page.
            if (!Settings.AllowOrphans && currentWebAlias.Equals(defaultWebAlias, StringComparison.InvariantCultureIgnoreCase))
            {
                HttpContext.Current.Response.Redirect(urlHelper.Action("webaliasrequired", "error"));
            }


            // If we are an orphan, try to redirect the user back to a previously-visited replicated site
            if (Settings.RememberLastWebAliasVisited
                && currentWebAlias.Equals(defaultWebAlias, StringComparison.InvariantCultureIgnoreCase)
                && !defaultWebAlias.Equals(lastWebAlias, StringComparison.InvariantCultureIgnoreCase))
            {
                HttpContext.Current.Response.Redirect(defaultPage);
            }


            // Attempt to authenticate the web alias
            var identityService = new IdentityService();
            HttpContext.Current.Items["OwnerWebIdentity"] = identityService.GetIdentity(currentWebAlias);
            if (HttpContext.Current.Items["OwnerWebIdentity"] != null)
            {
                if (Settings.RememberLastWebAliasVisited && currentWebAlias.ToLower() != GlobalSettings.ReplicatedSites.DefaultWebAlias.ToLower())
                {
                    GlobalUtilities.SetLastWebAlias(currentWebAlias);
                }
                else
                {
                    GlobalUtilities.DeleteLastWebAlias();
                }
            }
            else
            {
                if (Settings.RememberLastWebAliasVisited)
                {
                    GlobalUtilities.DeleteLastWebAlias();
                    lastWebAlias = defaultWebAlias;
                    HttpContext.Current.Response.Redirect(defaultPage);
                }
                else
                {
                    HttpContext.Current.Response.Redirect(urlHelper.Action("invalidwebalias", "error"));
                }
            }
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
            var authenticated = false;

            var authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];

            // Set the culture
            if (authCookie != null)
            {
                var identity = CustomerIdentity.Deserialize(authCookie.Value);
                if (identity == null)
                {
                    FormsAuthentication.SignOut();
                }
                else
                {
                    authenticated = true;

                    HttpContext.Current.User = new GenericPrincipal(identity, null);
                    Context.User = new GenericPrincipal(identity, null);


                    // Set the culture codes
                    GlobalUtilities.SetCurrentCulture(Identity.Customer.Market.CultureCode);
                }
            }
            else
            {
                var cultureCookie = HttpContext.Current.Request.Cookies[GlobalSettings.Globalization.LanguageCookieName];
                if (cultureCookie != null && cultureCookie.Value.IsNotNullOrEmpty())
                {
                    GlobalUtilities.SetCurrentCulture(cultureCookie.Value);
                }
            }

            // Set the language
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(Exigo.GetSelectedLanguage());
        }
    }
}