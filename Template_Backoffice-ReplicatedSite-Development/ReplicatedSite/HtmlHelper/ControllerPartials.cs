using System.Web.Mvc;

namespace ReplicatedSites.HtmlHelpers
{
    public static class ControllerPartialHtmlHelpers
    {
        public static MvcHtmlString Navigation(this HtmlHelper html, string controller = "", object model = null)
        {
            return Common.HtmlHelpers.ControllerPartials.ControllerPartial(html, "Navigations", controller, model);
        }
    }
}