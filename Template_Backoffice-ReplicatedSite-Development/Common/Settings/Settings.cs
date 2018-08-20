﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using ExigoService;
using Common.Api.ExigoWebService;
using Dapper;
using System.Data;

namespace Common
{
    public static class GlobalSettings
    {
        /// <summary>
        /// Exigo-specific API credentials and configurations
        /// </summary>
        public static class Exigo
        {
            public static bool EnviromentIsConfiguredCorrectly = true;
            /// <summary>
            /// Bubbles up a fatal error and reports to designated gatekeepers if failed
            /// </summary>
            public static void VerifyEnvironment(HttpContext context)
            {
                var debugEnvironments = ConfigurationManager.AppSettings["DebugEnvironments"].Split(',');
                var environmentGatekeepers = ConfigurationManager.AppSettings["EnvironmentGatekeepers"].Split(';');
                var releaseMode = ConfigurationManager.AppSettings["ReleaseMode"];
                var isLocal = context.Request.IsLocal;
                bool isValidEnv = (releaseMode.ToLower() == "debug" && debugEnvironments.Contains(context.Request.Url.Host.ToLower()))
                                    ||
                                  (releaseMode.ToLower() != "debug" && !debugEnvironments.Contains(context.Request.Url.Host.ToLower()));
                if (!isValidEnv)
                {
                    if (EnviromentIsConfiguredCorrectly)
                    {
                        Exigo.EnviromentIsConfiguredCorrectly = isValidEnv;
                        ExigoService.Exigo.SendEmail(new ExigoService.SendEmailRequest
                        {
                            To = (isLocal ? new[] { environmentGatekeepers.FirstOrDefault() } : environmentGatekeepers),
                            From = GlobalSettings.Emails.NoReplyEmail,
                            ReplyTo = new[] { GlobalSettings.Emails.NoReplyEmail },
                            SMTPConfiguration = GlobalSettings.Emails.SMTPConfigurations.Default,
                            Subject = "{0} - Deployment Environment Mismatch".FormatWith(GlobalSettings.Company.Name),
                            Body = @"
                    <p>
                        {1} has had the wrong environment settings deployed to the {0} server.
                    </p>
                    <p>
                        The site is currently dissabled to prevent damage to the {0} database.
                    </p>
"
                                .FormatWith(releaseMode, GlobalSettings.Company.Name)
                        });
                    }

                    throw new Exception("Environment Mismatch: The deployment platform is incorect for this instance {0} as {1}".FormatWith(context.Request.Url.Host.ToLower(), releaseMode.ToLower()));

                }
            }

            /// <summary>
            /// Cache time in minutes
            /// </summary>
            public static int CacheTimeout = 120;

            /// <summary>
            /// Web Session Settings
            /// </summary>
            public static class UserSession
            {
                /// <summary>
                /// Set to True for SQL session caching
                /// Set to False for Redis session caching
                /// </summary>
                public static bool UseDbSessionCaching = true;

                /// <summary>
                /// Set to true to use in Memory tables
                /// </summary>
                public static bool UseOLTPInMemory = true;

                public static int MinutesToLive = 1440;
                public static int DbExpireSessionTaskMilliSecDelay = 1800000;
            }

            public static class Caching
            {
                /// <summary>
                /// Redis Connection String. If you are going to use Redis, you must change this to the Client's Redis connection string.
                /// </summary>
                public static string RedisConnectionString = ConfigurationManager.AppSettings["Api.ConnectionStrings.Redis"];
                public static bool Enabled = true;
                public static int Lifespan = 300000;
                public static string Schema = "_cache";
            }

            /// <summary>
            /// Web service and SQL API credentials and configurations
            /// </summary>
            public static class Api
            {
                public static string LoginName = ConfigurationManager.AppSettings["Api.LoginName"];
                public static string Password = ConfigurationManager.AppSettings["Api.Password"];
                //Errors out without exception and won't authenticate when CompanyKey is incorrect
                public static string CompanyKey = ConfigurationManager.AppSettings["Api.CompanyKey"];

                public static bool UseSandboxGlobally = bool.Parse(ConfigurationManager.AppSettings["Api.UseSandboxGlobally"]);
                public static int SandboxID
                {
                    get
                    {
                        if (UseSandboxGlobally)
                        {
                            return int.Parse(ConfigurationManager.AppSettings["Api.SandboxID"]);
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }

                /// <summary>
                /// Replicated SQL connection strings and configurations
                /// </summary>
                public static class Sql
                {
                    public static class ConnectionStrings
                    {
                        public static string SqlReporting = ConfigurationManager.ConnectionStrings["Api.Sql.ConnectionStrings.SqlReporting"].ConnectionString;
                    }
                }
            }

            /// <summary>
            /// Payment API credentials
            /// </summary>
            public static class PaymentApi
            {
                public static string LoginName = "exigodemo_w77ipWL41";
                public static string Password = "594bIToTPmPe7W574IcoGzTg";
            }

            /// <summary>
            /// Content Manager Toggle
            /// </summary>
            public static class ContentManager
            {
                public static bool IsEditModeOn = true;
            }
        }

        /// <summary>
        /// Default backoffice settings
        /// </summary>
        public static class Backoffices
        {
            public static string GoogleAnalyticsWebPropertyID = "UA-66364312-5";

            public static int SessionTimeout = 20; // In minutes            
            /// <summary>
            /// This is the customer ID of the Content Manager Administrator and the one that has
            /// access to the resource files
            /// </summary>
            public static int AdministratorID = 1;

            /// <summary>
            /// Silent login URL's and configurations
            /// </summary>
            public static class SilentLogins
            {
                public static string DistributorBackofficeUrl = Company.BaseBackofficeUrl + "/silentlogin/?token={0}";
                public static string RetailCustomerBackofficeUrl = Company.BaseReplicatedUrl + "/{0}/account/silentlogin/?token={1}";
            }

            /// <summary>
            /// Waiting room configurations
            /// </summary>
            public static class WaitingRooms
            {
                /// <summary>
                /// The number of days a customer can be placed in a waiting room after their initial enrollment.
                /// </summary>
                public static int GracePeriod = 30;
            }

            public class CompanyNews
            {
                public static int[] Departments
                {
                    get
                    {
                        int[] response = new int[0];
                        // Multiple Departments May be used via comma seperated list of ID #'s. By passing "", all News Items that are set as 'AvailableInBackOffice' will return.
                        string configDepartments = "1,2,3,4,5,6";
                        if (!configDepartments.IsNullOrEmpty())
                        {
                            response = Array.ConvertAll(configDepartments.Split(','), int.Parse);
                        }
                        return response;
                    }
                }
            }

            public static class Reports
            {
                /// <summary>
                /// Parameters for standard reports
                /// </summary>
                public static class Dashboard
                {
                    public static class NewestDistributors
                    {
                        // Hard coded for demo purposes, change to CustomerType lookups when implementing new client.
                        public static List<int> CustomerTypes = new List<int> { Common.CustomerTypes.Distributor };
                        public static List<int> CustomerStatuses = new List<int> { CustomerStatusTypes.Active };
                        //Setting to 0 will ignore the date filter and grab the top X amount regardless of how old they are
                        public static int Days = 0;
                        // number of days in the past
                        public static int MaxResultSize = 12;
                    }

                    public static class CompanyNews
                    {
                        public static int Department
                        {
                            get
                            {
                                int response = 0;
                                // Multiple Departments May be used via comma seperated list of ID #'s. By passing "", all News Items that are set as 'AvailableInBackOffice' will return.
                                string configDepartments = "1";
                                if (!configDepartments.IsNullOrEmpty())
                                {
                                    response = int.Parse(configDepartments);
                                }
                                return response;
                            }
                        }

                    }
                }
                public static class NewestDistributors
                /// <summary>
                /// Newest Distributors Report on the dashboard
                /// </summary>
                {
                    // Hard coded for demo purposes, change to CustomerType lookups when implementing new client.
                    public static List<int> CustomerTypes = new List<int> { Common.CustomerTypes.Distributor };
                    public static List<int> CustomerStatuses = new List<int> { CustomerStatusTypes.Active };
                    //Setting to 0 will ignore the date filter and grab the top X amount regardless of how old they are
                    public static int Days = 0;
                }

                public static class CommissionReportCacheRefresh
                //kicks off a async endless looping task to update an order in the database and call the rankqual api to ensure the commission report cache doesn't expire.
                //This is really only needed on sites that don't receive traffic for 12+ hours at a time and also haven't received a single order update int that same timeframe (e.g. demo and testing sites)
                {
                    public static bool UseTaskToForceReportCacheRefresh = true;
                    public static int taskWaitTime = 60; //In Minutes
                }
            }
        }

        /// <summary>
        /// Default replicated site settings
        /// </summary>
        public static class ReplicatedSites
        {
            public static int SessionTimeout = 20;
            public static string DefaultWebAlias = "CorpOrphan";
            public static int DefaultAccountID = 2;
            public static int IdentityRefreshInterval = 15; // In minutes
            public static string FormattedBaseUrl = Company.BaseReplicatedUrl + "/{0}";
            public static string GoogleAnalyticsWebPropertyID = "UA-66364312-6";
            public static bool SkipJoinEnroller = true;

            public static string GetFormattedUrl(string webAlias)
            {
                return FormattedBaseUrl.FormatWith(webAlias);
            }
            public static string EnrollmentUrl = FormattedBaseUrl + "/enrollment";
            public static string GetEnrollmentUrl(object webAlias)
            {
                return string.Format(EnrollmentUrl, webAlias);
            }

            /// <summary>
            /// Comma seperated Web Category IDs
            /// </summary>
            public static string DefaultHomePageProducts = "";
            public static string TopXHomePageProducts = "5";
            public static string HomePageProductsWebCat = "63";
        }

        /// <summary>
        /// Default items settings
        /// </summary>
        public static class Items
        {
            public static int WebID = 1;
        }

        /// <summary>
        /// Market configurations used for orders, autoOrders, products and more
        /// </summary>
        public static class Markets
        {
            //JS, 09/11/2015
            //Removed the Comma because Commas will break Cookie Names in Safari
            public static string MarketCookieName = Globalization.CookieKey + "SelectedMarket";

            public static List<Market> AvailableMarkets
            {
                get
                {
                    return new List<Market>
                    {
                        new CanadaMarket(),
                        new UnitedStatesMarket()
                    };
                }
            }
        }

        /// <summary>
        /// Language and culture code configurations
        /// </summary>
        public static class Globalization
        {
            public static string CookieKey = "ExigoDemo";
            public static string CountryCookieName = CookieKey + "SelectedCountry";
            public static string CountryCookieChosenName = CookieKey + "CountryChosen";
            public static string SiteCultureCookieName = CookieKey + "SiteCulture";
            public static string LanguageCookieName = CookieKey + "SelectedLanguage";
            public static string LogInAlertCookieName = CookieKey + "LogInAlert";
        }

        /// <summary>
        /// Language and culture code configurations
        /// </summary>
        public static class AutoOrders
        {
            public static List<int> AvailableFrequencyTypeIDs
            {
                get
                {
                    return new List<int>
                    {
                        FrequencyTypes.Monthly
                    };
                }
            }
            public static List<FrequencyType> AvailableFrequencyTypes = AvailableFrequencyTypeIDs.Select(c => ExigoService.Exigo.GetFrequencyType(c)).ToList();
        }

        /// <summary>
        /// Customer avatar configurations
        /// </summary>
        public static class Avatars
        {
            public static string DefaultAvatarAsBase64 = "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAEsASwDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD0WiiigAooooAKKKKACiiigBO1LRRQAneloooAKKKKAEpaKKACiiigApO1LRQAUUUUAFFFFABRRRQAUUUUAJ3paKKACiiigAoopO1AC0UUUAFFFFABRSd6WgAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAopKWgBOKWiigAooooAKKTvS0AJS0UUAFJS0UAFFFFABRRRQAUnelooAKKKKACiiigApO9LRQAnaloooAKKKKACiiigBKKWigAopO9LQAUlLRQAUUUUAFJS0UAJS0UUAFFFFACUtFFABSUtFABSUtJQAtFFFABRRRQAUUUUAFFFFABRRRQAUUUnegBaKKKACinRxvK4SNSzHsK17bQJGwbiTYP7q8mgDGpyo7/AHULfQV1cOl2cH3YQx9W5q4FVRhQB9KAOMFpdHpbSn/gBprW06fehkH1Q121FAHCkYNFdrJbwyjEkSN9VrPn0O1lyY90Te3IoA5qir11pVzaguV3oP4lqjQAUUneloAKKKKACiiigAooooAKKKKACiiigAooooAKKTvS0AFFFFABRRRQAUnalooAKKKKACiiigAq9YaZJetuOUiHVvX6U/S9NN3J5knEKn/vr2rp1VUUKoAUcADtQBFbWkNomyFMep7mp6KKACiiigAooooAKKKKACsq/wBHiuQXhAjl/Rq1aKAOIlieCQxyKVYdQaZXXX9hHfRYPEgHyt6Vyk0TwStHIuGWgBlFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFACd6WiigAooooAKKKKACk5paKACrFlaNeXKxDherH0FV66jRrUW9mJCP3kvzH6dqAL8USQxLGgwqjAFPoooAKKKKACikpaACiiigAooooAKKKKACszV7D7TD5qD96g7fxD0rTooA4Wir+r2gtbzKDCSfMPaqFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQBPZQ/abyOLszc/SuyAwMDpXO+H4g11JIf4VwPxro6ACiiigAooooAKKO1HagAooooAO9HakpaACiiigAooooAzdat/OsGcfejO4f1rl67eRBJE6HoykGuJZdrFT1BwaAEooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAoopO1AHQ+Hl/cTN6uBW1WP4e/49JR/00/pWxQAUUUUAFFFJ3oAWiiigAooooAKKKKACiiigAooooAK4y+XbfTr/tmuzrjtROdRuP8AfNAFaiiigAooooAKKKKACiiigAooooAKKKKACkpaKACiiigAooooAKKKKAN3w6/+vj+jVu1y2izeVqKqTxICtdTQAUUUUAFFJS0AFFFFABRRRQAUUUUAFFFFABRRRQAVxVw/mXMr/wB5yf1rrL6b7PZTSdwpx9a46gAooooAKKKKACiiigAooooAKKKKAE70tFFABRRRQAUUUnegBaKKKACik7UtACo5jkV14ZTkV2dvMLi3SVejDNcXW1oV7tc2rn5W5T60AdBRRRQAUUdqKACikpaACiiigAooooAO1FFFABRRUU0yQQtK5wqjJoAyNfucLHbA9fmb+lYNS3E73M7zP95j+VRUAFJ2paTvQAtFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFKrFWDA4I5BpKKAOq0zUFvIcMcTL94evvWhXEQzPBKssbbWXvXT6fqUV6m0kLMOq+v0oA0KKKKACiiigAooooAKKKKACiimswRSzEBR1JoAUkAZNczq+ofapPJiP7lD1/vGpNU1bz8wW5Ij6M396sigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACk70tFABRRRQAUUUnegBaKKKAClVmRgykhh0Iq5Z6XcXnzAbI/7zD+Vb1ppVta4bb5kn95qAINMvLudQs0DFe0vStaiigAopKWgAooooAKKKKAGSu0cZZELkdFB61y+o311cSFJlaJR/BjFdXUUsEc6bZUDr7igDiqK3bvQOr2r/APAG/wAaxZI3hcpIhVh2IoAZRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUVJBBJcSrHEu5jQAxEeRwiKWY8ACugsNFSLElzh36hewq3YadHZJnhpT95sfyq9QAgAAwKWiigAooooAKKSloAKKKKACiiigAooo7UAFV7mzhu02ypn0PcVYooA5K+02WybP34j0cD+dUq7hlV1KsAVPUGuc1PSTbZmgBMXcf3aAMqiiigAooooAKKKKACiiigAooooAKKKTtQAtFFFABRSUtABRRRgmgB8UTzSrHGu5m4ArqtPsUsYcDmRvvNUOk6eLWLzZB++f8A8dHpWnQAUUUd6ACjtRRQAUlFLQAUUUUAFFFFABRRRQAUUUUAFFFFABSEAjB6UtFAHNarpn2ZjNEP3THkf3ayq7h0WRCrgMp4INcnqNi1lcYHMbcqf6UAU6KKKACiiigAooooAKKKKACiik7UALRRRQAlFLRQAVr6LY+dL9okHyIflHqazLeB7idIU+8xxXY28K28CRIAFUYoAlooooAKKKKACiiigAooooAKO1FFABRRRQAUUUUAFFFFABRRRQAUUUUAFVr21S8tmibg9VPoas0UAcPJG0UjRuMMpwRTa3des+l0g9n/AKGsKgAooooAKKKKACiiigAooooAKKKKACiiljRpJFRclmOBQBu6BagK10w5Pyp/WtyoreFbeBIl6KMVLQAUUUUAFFFHagApKWigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKAGSxrNE0bjKsMGuMuImt7h4m6qcV21c/4gttskdwo4b5W+tAGLRRRQAUUUUAFFFFACd6WiigAopKWgArT0O382+8wj5Yxu/HtWZXSaDDssmkPWRv0FAGtRR2ooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAqpqMH2mxlTGTjK/UVbooA4Wip72HyL2aPsG4+lQUAFFFFABRRRQAUUUUAFFFFABXZWUfk2UMfogz9a5GBPMuI0/vOBXa0ALRRRQAUlLRQAUUUUAFFFFABRRSUAHeloooAKKKKACiiigAo7UUUAFFFFABR2oooAKKKKACiiigDmtfjC3qPj76frWVXQeIY8wQyf3WxXP0AFFFFABRRRQAUUUUAFFJ2ooAuaWu/U4B/tZ/KuvrldF51SP2B/lXVUAFHaiigA70UlLQAUUd6O9ABRRRQAlLRR3oAKKKKACiiigAooooAKKKKACiiigAoo70UAFFFFABRRR3oAzdcXdpjn+6wNcvXW6tzpc/0H865KgAooooAKKTvS0Af//Z";
        }

        /// <summary>
        /// Error logging configuration
        /// </summary>
        public static class ErrorLogging
        {
            public static bool ErrorLoggingEnabled = false;
            public static string[] EmailRecipients = new List<string> { "matte@exigo.com" }.ToArray();
        }

        /// <summary>
        /// Email configurations
        /// </summary>
        public static class Emails
        {
            public static string NoReplyEmail = "noreply@exigonow.com";
            public static string ContactUsEmail = "noreply@exigonow.com";
            public static string VerifyEmailUrl = Company.BaseBackofficeUrl + "/verifyemail";

            // NEED NEW CREDS FROM CLIENT IF THEY ARE TO SEND ANY EMAILS FROM THE WEB
            public static class SMTPConfigurations
            {
                public static SMTPConfiguration Default = new SMTPConfiguration
                {
                    Server = "mail.exigo.com",
                    Port = 26,
                    Username = "noreply@exigonow.com",
                    Password = "whodaman",
                    EnableSSL = false
                };
            }
        }

        /// <summary>
        /// Company information
        /// </summary>
        public static class Company
        {
            public static int CorporateCalendarAccountID = 1;
            public static string Name = "Exigo Office, Inc.";

            public static Address Address = new Address()
            {
                Address1 = "8130 John Carpenter Freeway",
                Address2 = "",
                City = "Dallas",
                State = "TX",
                Zip = "75247",
                Country = "US"
            };
            public static string Phone = "(555)555-5555";
            public static string Email = "info@exigo.com";
            public static string Facebook = "http://www.facebook.com/";
            public static string Twitter = "http://twitter.com/";
            public static string YouTube = "http://youtube.com/";
            public static string Blog = "http://blogger.net/blog/";
            public static string Pinterest = "http://www.pinterest.com";
            public static string Instagram = "http://www.instagram.com";
            public static string DefaultCompanyMessage = "This is our company statement.";


            public static string BaseBackofficeUrl = ConfigurationManager.AppSettings["Company.BaseBackofficeUrl"];
            public static string BaseReplicatedUrl = ConfigurationManager.AppSettings["Company.BaseReplicatedUrl"];
        }

        /// <summary>
        /// EncryptionKeys used for silent logins and other AES encryptions
        /// </summary>
        public static class EncryptionKeys
        {
            public static string General = "SDCLKJYAFS654ASF321FP87K"; // 24 characters 

            public static class SilentLogins
            {
                public static string Key = GlobalSettings.Exigo.Api.CompanyKey + "silentlogin";
                public static string IV = "kjJ6F6sf84vfV432"; // Must be 16 characters long
            }
        }

        public static class RegularExpressions
        {

            public const string EmailAddresses = @"^[^@]+@[^@]+\.[^@]+$";  // Has only one @ symbol, at least one character before the @, at least one character between the @ and the period, and at least one character after the period
            public const string LoginName = "^[a-zA-Z0-9]{3,}$";
            public const string Password = "^.{1,50}$";
            public const string PhoneNumber = @"\d{10,11}$";


            public const string BankAccountNumber = @"^\d{1,15}$";
            public const string BankRoutingNumber = @"^\d{9}$";
            public const string CVV = @"\d{3,4}$";


            // TaxID expressions by country           
            public const string UnitedStatesTaxID = @"^\d{9}$";
            public const string CanadaTaxID = @"^\d{9}$";
        }

        /// <summary>
        /// Debug Module Configuration
        /// </summary>
        public static class Debug
        {
            //JS, 09/15/2015
            //Added Cookie Name for Debug Parameters
            public static string DebugCookieName = "exigolite_DebugMode";
        }

        /// <summary>
        /// A Collection of API Keys for Google Services
        /// </summary>
        public static class Google
        {
            /// <summary>
            /// The Google Maps API Key for the Client
            /// </summary>
            public static string Maps = "AIzaSyBzS3gskQJO-01JoBg8TOpr1aTvyKIK-6Y";
        }

    }

    public enum MarketName
    {
        UnitedStates,
        Canada
    }
    public enum AvatarType
    {
        Tiny,
        Small,
        Default,
        Large
    }
    public enum SocialNetworks
    {
        Facebook = 1,
        GooglePlus = 2,
        Twitter = 3,
        Blog = 4,
        LinkedIn = 5,
        MySpace = 6,
        YouTube = 7,
        Pinterest = 8,
        Instagram = 9
    }

    public enum TreeTypes
    {
        Binary = 1,
        Unilevel = 2
    }

    public enum LogTypes
    {
        RedirectPaymentMethodOne = 1
    }

    public static class CustomerStatusTypes
    {
        public const int Active = 1;
        public const int Terminated = 2;
        public const int Inactive = 3;

    }
}
