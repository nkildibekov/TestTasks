using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using ExigoService;
using System.Web;
using System.Web.Caching;
using Common.Services;
using System.Text.RegularExpressions;

namespace Common.HtmlHelpers
{
    public static class EditorHtmlHelpers
    {
        public static RouteValueDictionary GetEditorHtmlAttributes(this HtmlHelper html, object defaults = null)
        {
            var viewData = html.ViewData;
            var results = new RouteValueDictionary();


            // Determine where all of our values are coming from
            var valueCollections = new List<RouteValueDictionary>();
            if (defaults != null) valueCollections.Add(new RouteValueDictionary(defaults));
            if (viewData["htmlAttributes"] != null) valueCollections.Add(new RouteValueDictionary(viewData["htmlAttributes"]));


            // Add some global attributes, if applicable
            var globalValues = new RouteValueDictionary();
            globalValues.Add("placeholder", html.ViewContext.ViewData.ModelMetadata.DisplayName);
            if (html.ViewContext.ViewData.ModelMetadata.IsRequired)
            {
                globalValues.Add("required", "true");
                globalValues.Add("aria-required", "true");
            }
            if (globalValues.Count > 0) valueCollections.Add(globalValues);


            // Merge the collections together
            if(valueCollections.Count > 0)
            {
                foreach (var valueCollection in valueCollections)
                {
                    foreach (var item in valueCollection)
                    {
                        var key = item.Key.ToLower().Replace("_", "-");

                        if (results.ContainsKey(key))
                        {
                            results[key] = string.Format("{0} {1}", results[key], item.Value);
                        }
                        else
                        {
                            results.Add(key, item.Value);
                        }
                    }
                }
            }

            return results;
        }


        public static IDisposable BeginHtmlFieldPrefixScope(this HtmlHelper html, string htmlFieldPrefix)
        {
            return new HtmlFieldPrefixScope(html.ViewData.TemplateInfo, htmlFieldPrefix);
        }
        private class HtmlFieldPrefixScope : IDisposable
        {
            private readonly TemplateInfo templateInfo;
            private readonly string previousHtmlFieldPrefix;

            public HtmlFieldPrefixScope(TemplateInfo templateInfo, string htmlFieldPrefix)
            {
                this.templateInfo = templateInfo;

                previousHtmlFieldPrefix = templateInfo.HtmlFieldPrefix;
                templateInfo.HtmlFieldPrefix = htmlFieldPrefix;
            }

            public void Dispose()
            {
                templateInfo.HtmlFieldPrefix = previousHtmlFieldPrefix;
            }
        }

        /// <summary>
        /// Return cached version of a content block for a given country code and language.
        /// </summary>
        /// <param name="contentItemID">GUID</param>
        /// <param name="customerID">Used to check if user is Administrator to enable content block edit option.</param>
        /// <param name="currentCountry">User's current country.</param>
        /// <param name="selectedLanguage">User's selected language.</param>
        /// <returns></returns>
        public static HtmlString ContentBlock(string contentItemID, string siteDescription, string selectedLanguage, string currentCountry = "US", int customerTypeID = 0)
        {
            // Get the data and cache it.
            var market = GetContentMarket(currentCountry);
            var language = GetContentLanguage(market, selectedLanguage);
            string cacheKey = contentItemID.ToUpper() + market.CookieValue + language.LanguageID.ToString();
            if (HttpRuntime.Cache[cacheKey] == null)
            {
                var contentItemRequest = new GetContentItemRequest
                {
                    ContentItemIDs = new[] { contentItemID },
                    SiteDescription = siteDescription
                };
                var item = ContentService.GetContentBlock(contentItemRequest).FirstOrDefault();
                string content = null;
                // If content is available in contentItem table, we check in contentItemCountryLanguages
                if (item != null)
                {
                    var request = new GetContentItemCountryLanguagesRequest
                    {
                        ContentItemIDs = new[] { contentItemID },
                        CountryCode = market.CookieValue,
                        LanguageID = language.LanguageID,
                    };
                    content = ContentService.GetContentBlock(request).FirstOrDefault().Content.ToString();
                }
                if (content != null)
                {
                    HttpRuntime.Cache.Insert(cacheKey, content, null, DateTime.Now.AddMinutes(GlobalSettings.Exigo.CacheTimeout), Cache.NoSlidingExpiration);
                }
            }

            // Retrieve the cache.
            var data = HttpRuntime.Cache[cacheKey];
            var cacheToString = data == null ? "Could not fetch data. Please try again later." : data.ToString();

            cacheToString = RenderViewSyntax(cacheToString, cacheKey);

            // Assembles content block container.
            string rawContentContainer = "<div class='cmcontent'>{0}</div>";

            // If we are content admin, wrap content block in html that allows edit.
            //if (customerTypeID == CustomerTypes.Master)
            if (customerTypeID == CustomerTypes.PreferredCustomer) // Ask Matt
                {
                rawContentContainer = "<div  id='" + contentItemID + "' class='cmcontentedit' data-toggle='modal' data-target='#editContent'><span class='glyphicon glyphicon-edit'></span>{0}</div><div style='clear: both'></div>".FormatWith(rawContentContainer);
            }

            // Return the formatted content.
            return MvcHtmlString.Create(rawContentContainer.FormatWith(cacheToString));
        }

        public static string RenderViewSyntax(string viewContext, string cacheKey)
        {
            // Return the result
            string cleanHtml = InterpolateRazor(viewContext);
            string html = HttpUtility.HtmlDecode(cleanHtml);
            //enforce tag rules
            html = HtmlSanitizer.SanitizeHtml(html);
            HttpRuntime.Cache.Insert(cacheKey, html, null, DateTime.Now.AddMinutes(GlobalSettings.Exigo.CacheTimeout), Cache.NoSlidingExpiration);
            return html;
        }

        public static string InterpolateRazor(string input)
        {
            input = input.Replace("&quot;", "\"");
            //regex ;_;
            Dictionary<string,string> patterns = new Dictionary<string, string>()
            { 
                {"reource", "(@Resources\\.Common\\.\\w+)"},
                {"action", "(@Url\\.Action\\((\\s)?\\\"\\w+\\\"((\\s)?\\,(\\s)?\\\"\\w+\\\"(\\s)?)?\\))"}
            };
            foreach (var pattern in patterns)
                foreach (Match match in Regex.Matches(input, pattern.Value, RegexOptions.IgnoreCase))
                    input = input.Replace(match.Groups[1].Value, RazorPuppet.GetDynamic(match.Groups[1].Value.Replace(" ", "")));
            return input;
        }

        // Helper Methods
        private static Market GetContentMarket(string countryCode)
        {
            var availableMarket = GlobalSettings.Markets.AvailableMarkets.Where(c => c.CookieValue == countryCode).FirstOrDefault();
            if (availableMarket == null)
            {
                availableMarket = GlobalSettings.Markets.AvailableMarkets.Where(c => c.CookieValue == CountryCodes.UnitedStates).FirstOrDefault();
            }
            if (availableMarket == null)
            {
                availableMarket = GlobalSettings.Markets.AvailableMarkets.FirstOrDefault();
            }
            return availableMarket;
        }


        private static ExigoService.Language GetContentLanguage(Market market, string cultureCode)
        {
            var language = market.AvailableLanguages.Where(c => c.CultureCode == cultureCode).FirstOrDefault();
            if (language == null)
            {
                language = market.AvailableLanguages.Where(c => c.LanguageID == Languages.English).FirstOrDefault();
            }
            if (language == null)
            {
                language = market.AvailableLanguages.FirstOrDefault();
            }
            if(language == null && market.CookieValue != CountryCodes.UnitedStates)
            {
                market = GetContentMarket(CountryCodes.UnitedStates);
                return GetContentLanguage(market, cultureCode);
            }
            return language;
        }
    }
}