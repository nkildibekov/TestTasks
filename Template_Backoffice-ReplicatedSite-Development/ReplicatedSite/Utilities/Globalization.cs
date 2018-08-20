using Common;
using ExigoService;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;

namespace ReplicatedSite
{
    public static class Utilities
    {
        public static Language GetUserLanguage(HttpRequestBase request)
        {
            // Check for the cookie first
            var cookie = request.Cookies[GlobalSettings.Globalization.LanguageCookieName];
            if (cookie != null && !cookie.Value.IsEmpty())
            {
                return Exigo.GetLanguageByCultureCode(cookie.Value);
            }

            // If we're signed in, let's use their account's language preference
            if (request.IsAuthenticated)
            {
                return Exigo.GetLanguageByID(Identity.Customer.LanguageID);
            }

            // Nothing yet? We must be dealing with an orphan who's never visited the site before.
            // Let's use their machine default, assuming we support that language.
            var cultureInfo = GlobalUtilities.GetRequestedLanguageCultureInfo(request);
            return Exigo.GetLanguageByCultureCode(cultureInfo.Name);
        }

        /// <summary>
        /// Gets the market the website is currently using.
        /// </summary>
        /// <returns>The Market object representing the current market.</returns>
        public static Market GetCurrentMarket()
        {
            // Get the user's country to see which market we are in
            var country = Common.GlobalUtilities.GetSelectedCountryCode();

            if (country.IsNullOrEmpty())
            {
                country = GlobalSettings.Markets.AvailableMarkets.Where(c => c.IsDefault == true).FirstOrDefault().Countries.FirstOrDefault();
            }
                       
            // If the country cookie in null or empty then create it
            var countryCookie = Common.GlobalUtilities.SetSelectedCountryCode(country);

            var market = GlobalSettings.Markets.AvailableMarkets.Where(c => c.Countries.Contains(country)).FirstOrDefault();

            // If we didn't find a market for the user's country, get the first default market
            if (market == null) market = GlobalSettings.Markets.AvailableMarkets.Where(c => c.IsDefault == true).FirstOrDefault();

            // If we didn't find a default market, get the first market we find
            if (market == null) market = GlobalSettings.Markets.AvailableMarkets.FirstOrDefault();

            // Return the market
            return market;
        }

        public static bool IsContentManagerAdmin(HttpRequestBase request)
        {
            //return (request.IsAuthenticated && (HttpContext.Current.User.Identity as ReplicatedSite.CustomerIdentity).CustomerTypeID == CustomerTypes.Master);
            return (request.IsAuthenticated && (HttpContext.Current.User.Identity as ReplicatedSite.CustomerIdentity).CustomerTypeID == CustomerTypes.PreferredCustomer);
        }
    }
}