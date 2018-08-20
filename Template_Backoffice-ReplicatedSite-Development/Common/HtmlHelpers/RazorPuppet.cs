using System;
using System.Linq;

namespace Common.HtmlHelpers
{
    public static class RazorPuppet
    {
        /// <summary>
        /// Decides what type of call this is
        /// </summary>
        /// <param name="target">String scraped from regex</param>
        /// <returns>Computed string</returns>
        public static string GetDynamic(string target)
        {
            try
            {
                if (target.StartsWith("@Resources.Common."))
                {
                    return GetResource(target); //calling a resource value
                }
                else
                {
                    return GetAction(target); //requesting an action url
                }
            }
            catch (Exception ex)
            {
                return target; //maybe bad regex match, so don't eat the text
            }
        }

        /// <summary>
        /// Gets a resource value from a string call to that value
        /// </summary>
        /// <param name="key">Regex matched string</param>
        /// <returns>The resource value, or the original string match if value not present</returns>
        public static string GetResource(string key)
        {
            global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Resources.Common", global::System.Reflection.Assembly.Load("App_GlobalResources"));
            return temp.GetString(key.Split('.').LastOrDefault());
        }

        /// <summary>
        /// Resolves the url route to a given action and controller based on a captured string
        /// </summary>
        /// <param name="target">Regex match of the Url.Action call</param>
        /// <returns>The correct route url as a string</returns>
        public static string GetAction(string target)
        {
            string[] actionParts = target.Replace("@Url.Action(", "").Replace("\")", "\"").Split(',');
            return actionParts.Length > 1 ? UrlCompileHelper.Action(actionParts[0].Trim('\"'), actionParts[1].Trim('\"')) : UrlCompileHelper.Action(actionParts[0].Trim('\"'));
        }
    }
}
