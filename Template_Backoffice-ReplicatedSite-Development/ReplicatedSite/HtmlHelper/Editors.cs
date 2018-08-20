using System.Web.Mvc;
using System.Web;
using ExigoService;

namespace ReplicatedSite.HtmlHelpers
{
    public static class EditorHtmlHelpers
    {
        /// <summary>
        /// Pedro Martinez, 2/3/2017, 79243:
        /// Intermediary Html helper that takes in content item ID, calculates current site description, selected languagte, current country and customer type ID.
        /// Sends this information to ContentBlock HtmlHelper in common.
        /// </summary>
        /// <param name="contentItemID">GUID</param>
        /// <returns></returns>
        public static HtmlString ContentBlock(this HtmlHelper helper, string contentItemID)
        {
            string siteDescription = Common.ContentSites.Replicated;
            string selectedLanguage = Utilities.GetUserLanguage(new HttpRequestWrapper(HttpContext.Current.Request)).CultureCode;
            string currentCountry = Exigo.GetSelectedLanguage();
            int customerTypeID = !HttpContext.Current.User.Identity.IsAuthenticated ? 0 : (HttpContext.Current.User.Identity as CustomerIdentity).CustomerTypeID;

            var contentBlock = Common.HtmlHelpers.EditorHtmlHelpers.ContentBlock(contentItemID, siteDescription, selectedLanguage, currentCountry, customerTypeID);
            return contentBlock;
        }
    }
}