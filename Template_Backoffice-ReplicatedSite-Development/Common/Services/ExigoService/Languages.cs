using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Dapper;


namespace ExigoService
{
    public static partial class Exigo
    {
        public static IEnumerable<Language> GetLanguages()
        {
            // Get a list of the available markets
            var availableLanguages = new List<Language>();
            var markets = GlobalSettings.Markets.AvailableMarkets;
            foreach (var market in markets)
            {
                foreach (var language in market.AvailableLanguages)
                {
                    if (!availableLanguages.Any(c => c.CultureCode == language.CultureCode))
                    {
                        availableLanguages.Add(language);
                    }
                }
            }

            // Get a list of the available languages from the avialble markets from above
            return availableLanguages.ToList();
        }

        public static Language GetLanguageByID(int languageID)
        {
            // Try to return the first available language we have 
            var result = GetLanguages().FirstOrDefault(c => c.LanguageID == languageID);

            // If we couldn't find it, get the languages and return it
            if (result == null) return GetLanguages().FirstOrDefault();

            return result;
        }

        public static string GetSelectedLanguage()
        {
            var defaultLanguage = GlobalSettings.Markets.AvailableMarkets.Where(c => c.IsDefault).FirstOrDefault().CultureCode;
            var languageCookie = HttpContext.Current.Request.Cookies[GlobalSettings.Globalization.LanguageCookieName];

            if (languageCookie == null)
            {
                languageCookie = new HttpCookie(GlobalSettings.Globalization.LanguageCookieName);
                languageCookie.Value = defaultLanguage;
                languageCookie.HttpOnly = false;
                HttpContext.Current.Response.Cookies.Add(languageCookie);
            }

            var language = Exigo.GetLanguages().FirstOrDefault(c => c.CultureCode == languageCookie.Value);
            if (language == null)
            {
                languageCookie.Value = defaultLanguage;
            }


            return languageCookie.Value;
        }

        public static int GetSelectedLanguageID(string language = "")
        {
            if (language == "")
            {
                language = GetSelectedLanguage();
            }

            switch (language)
            {
                case "es":
                case "es-US":
                    return (int)Languages.Spanish;
                case "en":
                case "en-US":
                default:
                    return (int)Languages.English;
            }
        }

        public static Language GetLanguageByCustomerID(int customerID)
        {
            var defaultCultureCode = GlobalSettings.Markets.AvailableMarkets.FirstOrDefault(c => c.IsDefault).CultureCode;
            var defaultLanguage = Exigo.GetLanguages().FirstOrDefault(c => c.CultureCode == defaultCultureCode).LanguageID;

            var languageID = defaultLanguage;

            // Get the user's language preference based on their saved preference
            if (HttpContext.Current.Request.IsAuthenticated)
            {
                using (var sqlContext = Exigo.Sql())
                {
                    languageID = sqlContext.Query<int>(@"select top 1 LanguageID from Customers where CustomerID = @customerID", new { customerID = customerID }).FirstOrDefault();
                }
            }

            var language = Exigo.GetLanguageByID(languageID);

            // If we couldn't find the user's preferred language, return the first one we find.
            if (language == null) language = Exigo.GetLanguages().FirstOrDefault();

            // Return the language
            return language;
        }

        public static Language GetLanguageByCultureCode(string cultureCode)
        {
            return GetLanguages().FirstOrDefault(c => c.CultureCode.Equals(cultureCode, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}