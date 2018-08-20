using AdminDashboard.Models;
using AdminDashboard.Services;
using AdminDashboard.ViewModels;
using Common;
using ExigoService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Dapper;


namespace AdminDashboard.Controllers
{
    public class RevenueController : Controller
    {
        public ActionResult Index()
        {
            if (Request.IsAjaxRequest()) return PartialView();
            else return View();
        }


        [HttpPost]
        public JsonNetResult GetGrossRevenueByCountry(DateTime startdate, DateTime enddate)
        {
            if (!GlobalUtilities.VerifySqlTableExists(GlobalSettings.Exigo.Api.Sql.ConnectionStrings.SqlReporting, "cache.GrossRevenueReport_OrderTotals"))
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = "Report not configured"
                });
            }


            dynamic data;


            // Get the data
            using (var context = Exigo.Sql())
            {
                data = context.Query(@"
                    SELECT 
                        c.CountryDescription as Country,
                        SUM(OrderCount) AS OrderCount,
                        SUM(Subtotal) AS SubTotal,
                        SUM(ShippingTotal) AS ShippingTotal,
                        SUM(TaxTotal) AS TaxTotal,
                        SUM(GrandTotal) AS GrandTotal,
                        SUM(CreditCard) AS CreditCard,
                        SUM(Cash) AS Cash,
                        SUM(PerkPoints) AS PerkPoints,
                        SUM(Product) AS Product,
                        SUM([Check]) AS [Check],
                        SUM(RebateCredits) AS RebateCredits,
                        SUM(WrapRewards) AS WrapRewards
                    FROM cache.GrossRevenueReport_OrderTotals o
                        INNER JOIN Countries c
	                        on c.CountryCode = o.Country
                    WHERE o.OrderDate BETWEEN @startdate AND DATEADD(dd,1,@enddate)
                    GROUP BY c.CountryDescription
                ", new
                 {
                     startdate = startdate,
                     enddate = enddate
                 }).ToList();
            }

            return new JsonNetResult(new
            {
                data = data
            });
        }

        [HttpPost]
        public JsonNetResult GetGrossRevenueByType(DateTime startdate, DateTime enddate)
        {
            if (!GlobalUtilities.VerifySqlTableExists(GlobalSettings.Exigo.Api.Sql.ConnectionStrings.SqlReporting, "cache.GrossRevenueReport_OrderTotals"))
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = "Report not configured"
                });
            }


            dynamic data; 

            // Get the data
            using (var context = Exigo.Sql())
            {
                data = context.Query(@"
                    SELECT  
                        c.CountryDescription as Country,
	                    ot.OrderTypeDescription as OrderType,
                        SUM(OrderCount) AS OrderCount,
                        SUM(Subtotal) AS SubTotal,
                        SUM(ShippingTotal) AS ShippingTotal,
                        SUM(TaxTotal) AS TaxTotal,
                        SUM(GrandTotal) AS GrandTotal,
                        SUM(CreditCard) AS CreditCard,
                        SUM(Cash) AS Cash,
                        SUM(PerkPoints) AS PerkPoints,
                        SUM(Product) AS Product,
                        SUM([Check]) AS [Check],
                        SUM(RebateCredits) AS RebateCredits,
                        SUM(WrapRewards) AS WrapRewards
                    FROM cache.GrossRevenueReport_OrderTotals o
                        INNER JOIN Countries c
	                        on c.CountryCode = o.Country
	                    INNER JOIN OrderTypes ot
	                        on ot.OrderTypeID = o.OrderType
                    WHERE OrderDate BETWEEN @startdate AND DATEADD(dd,1,@enddate)
                    GROUP BY c.CountryDescription, ot.OrderTypeDescription
                    ORDER BY c.CountryDescription, ot.OrderTypeDescription ASC
                ", new
                 {
                     startdate = startdate,
                     enddate = enddate
                 }).ToList();
            }

            return new JsonNetResult(new 
            {
                data = data
            });
        }
    }
}
