using Common.Api.ExigoWebService;
using ExigoService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Linq;
using System.Globalization;


namespace Common
{
    public static partial class GlobalUtilities
    {
        public static CultureInfo GetRequestedLanguageCultureInfo(HttpRequestBase request)
        {
            var userLanguages = request.UserLanguages;


            CultureInfo ci;
            if (userLanguages != null && userLanguages.Any())
            {
                try
                {
                    ci = new CultureInfo(userLanguages[0].Trim());
                }
                catch (CultureNotFoundException)
                {
                    ci = CultureInfo.InvariantCulture;
                }
            }
            else
            {
                ci = CultureInfo.InvariantCulture;
            }

            return ci;
        }
        public static string GetRequestedCountry(HttpRequestBase request)
        {
            var culture = GetRequestedLanguageCultureInfo(request);

            var regionInfo = new RegionInfo(culture.LCID);
            return regionInfo.TwoLetterISORegionName;
        }
    }
}