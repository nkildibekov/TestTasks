using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Common;
using Common.Models;
using Common.Services;
using ExigoService;
using Backoffice.Filters;
using Common.HtmlHelpers;
using System.Text.RegularExpressions;

/// <summary>
/// Content types: Log-In Alerts and Content Blocks.
/// Difference: Alerts can be available for specific country/language sets. Content Blocks have to be available in all available contry/language sets.
/// Data is stored in two ExtendedDatabase tables: ContentItems and ContentItemCountryLanguage.
/// Difference: ContentItem stores all properties of content item, except content since it may differ from language to language or country to country.
/// ContentItemCountryLanguage stores one entry of content item per country/language set with different content for each.
/// </summary>
namespace Backoffice.Controllers
{
    [ContentManagerAdminsFilter]
    [RoutePrefix("content")]
    public class ContentController : Controller
    {
        /// <summary>
        /// Check if customer type is master for initialization purposes. 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [IgnoreContentManagerAdminFilter]
        [AllowAnonymous]
        public JsonNetResult IsCustomerTypeMaster()
        {
            try
            {
                bool authenticated = Utilities.IsContentManagerAdmin(Request);

                return new JsonNetResult(new
                {
                    success = true,
                    authenticated
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets a list of languages according to the market passed in the parameter.
        /// Market is selected in a drop-down menu by CM Admin.
        /// </summary>
        /// <param name="market">Market object.</param>
        /// <returns></returns>
        [HttpPost]
        public JsonNetResult GetLanguages(string market)
        {
            try
            {
                var availableLanguages = ContentService.GetLanguages(market);

                // Return the list of languages available for this market.
                return new JsonNetResult(new
                {
                    success = true,
                    availableLanguages
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets a list of all available markets followed by their perspective available languages.
        /// To be displayed in the Alerts menu as a selector for market/language availability.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonNetResult GetCountryAndLanguageSets()
        {
            try
            {
                // Empty lists for country and language to be set as data attributes.
                List<string> availableCountries;
                List<int> availableLanguages;
                // An empty list that will store our sets of countries and languages to be displayed to user separated by '/'.
                List<string> availableCountryAndLanguageSets;

                ContentService.GetCountryAndLanguageSets(out availableCountries, out availableLanguages, out availableCountryAndLanguageSets);

                // Return country/language sets.
                return new JsonNetResult(new
                {
                    success = true,
                    availableCountryAndLanguageSets,
                    availableCountries,
                    availableLanguages
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Returns content of specific item.
        /// </summary>
        /// <param name="contentID"></param>
        /// <param name="countryID"></param>
        /// <param name="languageID"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonNetResult GetContentItemContent(string contentID, string countryID, int languageID)
        {
            try
            {
                var request = new GetContentItemCountryLanguagesRequest
                {
                    ContentItemIDs = new[] { contentID },
                    CountryCode = countryID,
                    LanguageID = languageID
                };
                // Get content item py passing contentID and selected country/language.
                var contentBlock = ContentService.GetContentBlock(request).FirstOrDefault();

                // If contentBlock does not exist, we retreive default of US-en
                if (contentBlock == null)
                {
                    request.CountryCode = CountryCodes.UnitedStates;
                    request.LanguageID = Languages.English;
                    contentBlock = ContentService.GetContentBlock(request).FirstOrDefault();
                }

                if (contentBlock == null)
                {
                    throw new Exception("Could not fetch data. Please try again later.");
                }
                var content = ContentService.GetContentBlock(request).FirstOrDefault().Content;

                return new JsonNetResult(new
                {
                    content,
                    success = true
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets alert, alert content and country/language paris to be edited in the edit modal.
        /// </summary>
        /// <param name="contentID"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonNetResult GetAlertToEdit(string contentID)
        {
            try
            {
                var contentItemRequest = new GetContentItemRequest
                {
                    ContentItemIDs = new[] { contentID },
                    SiteDescription = "Backoffice"
                };
                // Return the first item from ContentItems with the contentID passed as param.
                var item = ContentService.GetAlert(contentItemRequest).FirstOrDefault();

                var contentItemCountryLanguagesRequest = new GetContentItemCountryLanguagesRequest
                {
                    ContentItemIDs = new[] { contentID }
                };
                // Return list of items from ContentItemCountryLanguages with the contentID of request.
                var countryLanguagePairs = ContentService.GetAlert(contentItemCountryLanguagesRequest);

                // Save content from first or default content found in ContentItemountryLanguages. We get the first one only because content is the same for all country/language pairs.
                var content = countryLanguagePairs.FirstOrDefault().Content;

                // Empty list of country/language pairs.
                var countryLanguagePairsValues = new List<string>();
                if (countryLanguagePairs != null)
                {
                    // Join country/Language and pass back to marketLanguagePairs List to be displayed in 'Market:'
                    foreach (var countrylanguagepair in countryLanguagePairs)
                    {
                        var market = GlobalSettings.Markets.AvailableMarkets.Where(c => c.CookieValue.ToString() == countrylanguagepair.CountryID).FirstOrDefault();
                        countryLanguagePairsValues.Add(market.CookieValue.ToString() + "/" + market.AvailableLanguages.Where(c => c.LanguageID == countrylanguagepair.LanguageID).FirstOrDefault().LanguageID.ToString());
                    }
                }
                
                // Return alert to be displayed, content, and country/language pairs.
                return new JsonNetResult(new
                {
                    item,
                    content,
                    countryLanguagePairsValues,
                    success = true
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Returns all the alerts by filtering the content in the ContentItems table.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonNetResult GetAlertList()
        {
            try
            {
                // New, empty model.
                var model = new ContentListModel();

                // New AlertItem object with GetAllAlerts boolean set to true.
                var request = new GetContentItemRequest
                {
                    SiteDescription = "Backoffice",
                    GetAllAlerts = true
                };

                // Get all alerts from ContentItems table and store them in ContentList of model.
                model.ContentList = ContentService.GetAlert(request);

                // Pass model to partial and return as html string.
                var html = this.RenderPartialViewToString("partials/_alertlist", model);

                return new JsonNetResult(new
                {
                    success = true,
                    html = html,
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Edits content that already exists in the ContentItems table by calling ExigoService ModifyContentItem and passing an edited contentItem.
        /// </summary>
        /// <param name="contentItem">Object with updated content</param>
        /// <param name="editID">Content Item ID for content block which we want to edit.</param>
        /// <returns></returns>
        [HttpPost]
        public JsonNetResult SetContent(ContentItem contentItem)
        {
            try
            {
                // Update content item
                ContentItem editedContent = new ContentItem()
                {
                    ContentItemID = contentItem.ContentItemID,
                    Content = contentItem.Content,
                    CountryID = contentItem.CountryID,
                    LanguageID = contentItem.LanguageID

                };
                ContentService.SetContentBlock(editedContent);

                return new JsonNetResult(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Creates and returns alert ContentItem by receiving partial object, filling out the rest and passing it to two exigo services.
        /// Creates one entry in the ContentItem table through the CreateContentItem service.
        /// Creates one entry for each available country/language combination in the ContentItemCountryLanguage table through the CreateContentItemCountryLanguage service.
        /// </summary>
        /// <param name="content">CreateContent object.</param>
        /// <returns></returns>
        [HttpPost]
        public JsonNetResult SetAlert(ContentItem content)
        {
            try
            {
                ContentService.SetAlert(content);

                return new JsonNetResult(new
                {
                    success = true,
                });
            }
            catch (Exception ex)
            {                
                return new JsonNetResult(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Deletes content from the ContentItems database by retreiving it with the ExigoService GetContentItem
        /// and deletes it by passing the retrieved okbject to the ExigoService DeleteContentItem.
        /// </summary>
        /// <param name="contentItem"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonNetResult DeleteAlert(string contentItemID)
        {
            try
            {
                ContentService.DeleteAlert(contentItemID);

                return new JsonNetResult(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Filters alerts to display correct one based on date and user's current market.
        /// </summary>
        /// <param name="contentItemID"></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreContentManagerAdminFilterAttribute]
        [AllowAnonymous]
        public JsonNetResult FilterAlert()
        {
            try {
                var request = new GetContentItemRequest
                {
                    GetAlert = true,
                    SiteDescription = "Backoffice"
                };

                // Get a list of the alerts in the ContentItems table, filtered by expiration date and available date.
                List<ContentItem> contentList = ContentService.GetAlert(request);

                // If we did not returned alerts from the ContentItems table, we throw exception.
                if (!contentList.Any()) {
                    return new JsonNetResult(new
                    {
                        success = false,
                        message = "No alerts to display."
                    });
                }

                // Get the country descripton and languageID of the current user.
                var currentCountry = GlobalSettings.Markets.AvailableMarkets
                    .Where(c => c.CookieValue == Identity.Current.Country)
                    .FirstOrDefault();
                var currentLanguageID = currentCountry.AvailableLanguages
                    .Where(c => c.CultureCode == Exigo.GetSelectedLanguage())
                    .FirstOrDefault()
                    .LanguageID;

                // Else, we look for those alerts in the ContentItemCountryLanguages table.
                var alertFilterItem = new GetContentItemCountryLanguagesRequest
                {
                    CountryCode = Identity.Current.Country,
                    LanguageID = currentLanguageID,
                    ContentItemIDs = contentList.Select(c => c.ContentItemID.ToString()).ToArray(),
                    ContentItems = contentList
                };

                var filteredContentList = ContentService.GetAlert(alertFilterItem);

                // If we did not returned alerts from the ContentItemCountryLanguages table, we throw exception.
                if (!filteredContentList.Any())
                {
                    throw new Exception("No alerts to display.");
                }

                // Else, we display the most recent alert.
                var alertToDisplay = filteredContentList.Where(x => x.ContentItemID == contentList.OrderByDescending(c => c.ValidFrom).FirstOrDefault().ContentItemID).FirstOrDefault();

                return new JsonNetResult(new
                {
                    success = true,
                    alertToDisplay
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// To be used when clicking Preview button in content editor.
        /// Replaces double quotes in Url.Action with encoded doble quotes.
        /// As of now, it only receives the content and sends it back in a JsonNetResult but will include validation in the future.
        /// </summary>
        /// <param name="previewContent"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonNetResult PreviewContent(string previewContent)
        {
            try
            {
                //Check for doublle quotes in Url.Action and replace with encoded double quotes.
                Regex pattern = new Regex(Regex.Escape("@Url.Action(") + "(.*?)" + Regex.Escape(")"));
                MatchCollection matches = pattern.Matches(previewContent);

                foreach (Match m in matches)
                {
                    var sanitizedAction = m.Groups[1].Value.Replace("\"", "&quot;");
                    previewContent = previewContent.Replace(m.Groups[1].Value, sanitizedAction);
                }

                return new JsonNetResult(new
                {
                    previewContent = HtmlSanitizer.SanitizeHtml(previewContent),
                    success = true
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
