using Common.Routes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace AdminDashboard
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapMvcAttributeRoutes();

            routes.MapRouteLowerCase(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "app", action = "index", id = UrlParameter.Optional }
            );
        }
    }
}