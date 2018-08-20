using Common;
using Common.Services;
using Dapper;
using ExigoService;
using ReplicatedSite.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;

namespace ReplicatedSite.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var ids = GetDefaultHomePageProducts();
            var items = Exigo.GetItems(ids);
            if (items.Count == 0)
            {
                items = new List<Item>();
                var indexer = 0;
                foreach (var letter in new[] { "A", "B", "C", "D", "E" })
                {
                    indexer++;
                    items.Add(new Item()
                    {
                        ItemDescription = "Product " + letter,
                        ItemCode = "ItemCode" + letter,
                        LongDetail1 = (indexer < 3 ? Greeking.Paragraph : null)
                    });
                }
            }

            return View(items);
        }

        private int[] GetDefaultHomePageProducts()
        {
            if (string.IsNullOrEmpty(GlobalSettings.ReplicatedSites.DefaultHomePageProducts))
            {
                var languageID = Exigo.GetSelectedLanguageID();

                using (var context = ExigoService.Exigo.Sql())
                {
                    var sql = @"
                                SELECT top " + (GlobalSettings.ReplicatedSites.TopXHomePageProducts.IsNullOrEmpty() ? "5" : GlobalSettings.ReplicatedSites.TopXHomePageProducts) + @" 
                                    i.ItemID
                                    FROM [dbo].[items] i 
	                                join [dbo].[WebCategoryItems] wci
	                                on i.ItemID = wci.ItemID " +
                                (GlobalSettings.ReplicatedSites.HomePageProductsWebCat.IsNullOrEmpty() ? "" : ("where wci.WebCategoryID = " + GlobalSettings.ReplicatedSites.HomePageProductsWebCat));

                    return context.Query<int>(sql).ToArray();
                }
            }
            else return System.Array.ConvertAll(GlobalSettings.ReplicatedSites.DefaultHomePageProducts.Split(','), s => int.Parse(s));
        }

        public ActionResult About()
        {
            var model = new ContactViewModel();
            return View(model);
        }

        [HttpPost]
        public ActionResult About(ContactViewModel model)
        {
            try
            {
                var requestUrl = Request.Url.AbsoluteUri;

                // Send the email
                Exigo.SendEmail(new SendEmailRequest
                {
                    //Change to ContactUsEmail for production
                    //To = new[] { GlobalSettings.Emails.ContactUsEmailAddress },
                    To = new[] { model.Email },
                    // Use the WebService Send Email call when no SMTP credentials are present
                    UseExigoApi = true,
                    From = model.Email,
                    ReplyTo = new[] { Settings.ContactUsReplyAddress },
                    SMTPConfiguration = GlobalSettings.Emails.SMTPConfigurations.Default,
                    Subject = "Contact Us Email",
                    Body = @"
                        <h3>Contact Us Email</h3>    
                         <ul>
                            <li>Name: {0}</li>
                            <li>Phone: {1}</li>
                            <li>Email: {2}</li>
                            <li>Notes: {3}</li>
                            <li>From Url: {4}</li>
                         </ul>                                   
                    ".FormatWith(model.Name, model.Phone, model.Email, model.Notes, requestUrl)
                });

                return new JsonNetResult(new
                {
                    success = true,
                    model
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
    }
}