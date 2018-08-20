using Common.Api.ExigoWebService;
using AdminDashboard.Models;
using AdminDashboard.Services;
using AdminDashboard.ViewModels;
using Common;
using AdminDashboard.Models.FusionCharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ExigoService;
using Dapper;

namespace AdminDashboard.Controllers
{
    public class OverviewController : Controller
    {
        public ActionResult Index()
        {
            if (Request.IsAjaxRequest()) return PartialView();
            else return View();
        }


        [HttpPost]
        public JsonNetResult GetOrderTotals()
        {
            var model = new BusinessSummaryViewModel();


            // Get the data
            dynamic ordertotals;
            using (var context = Exigo.Sql())
            {
                var query = context.QueryMultiple(@"
               -- Orders Summary
                SELECT  
                    o.OrderStatusID, 
                    OrderCount = count(*), 
                    OrderTotal = COALESCE(sum(o.SubTotal), 0)
                FROM Orders o
                WHERE 
                    o.OrderDate >= @startdate AND o.OrderDate < @enddate
                GROUP BY 
                    o.OrderStatusID


                -- Pending Autoship Totals
                SELECT OrderTotal = COALESCE(sum(o.SubTotal), 0)
                FROM
	                Orders o
                WHERE
	                o.AutoOrderID IS NOT NULL
	                AND o.OrderStatusID IN (1, 5, 6)
                    AND o.OrderDate >= @startdate AND o.OrderDate < @enddate

                OPTION(MAXDOP 8)
            ", new
             {
                 startdate = DateTime.Now.BeginningOfMonth(),
                 enddate = DateTime.Now.AddMonths(1).BeginningOfMonth()
             });

                ordertotals = query.Read().ToList();
                model.PendingAutoshipsTotal = query.Read<decimal>().FirstOrDefault();
            }


            // Process the data
            foreach (var row in ordertotals)
            {
                switch ((int)row.OrderStatusID)
                {
                    case OrderStatuses.Accepted:
                        model.AcceptedOrdersTotal += row.OrderTotal;
                        model.AcceptedOrdersCount = row.OrderCount;
                        break;
                    case OrderStatuses.ACHDeclined:
                        model.DeclinedOrdersTotal += row.OrderTotal;
                        model.ACHDeclinedOrdersCount = row.OrderCount;
                        break;
                    case OrderStatuses.ACHPending:
                        model.ACHPendingOrdersTotal += row.OrderTotal;
                        model.ACHPendingOrdersCount = row.OrderCount;
                        break;
                    case OrderStatuses.Cancelled:
                        model.CancelledOrdersTotal += row.OrderTotal;
                        model.CancelledOrdersCount = row.OrderCount;
                        break;
                    case OrderStatuses.CCDeclined:
                        model.DeclinedOrdersTotal += row.OrderTotal;
                        model.CCDeclinedOrdersCount = row.OrderCount;
                        break;
                    case OrderStatuses.CCPending:
                        model.CCPendingOrdersTotal += row.OrderTotal;
                        model.CCPendingOrdersCount = row.OrderCount;
                        break;
                    case OrderStatuses.Incomplete:
                        model.DeclinedOrdersTotal += row.OrderTotal;
                        model.IncompleteOrdersCount = row.OrderCount;
                        break;
                    case OrderStatuses.Pending:
                        model.CCPendingOrdersTotal += row.OrderTotal;
                        model.PendingOrdersCount = row.OrderCount;
                        break;
                    case OrderStatuses.Printed:
                        model.AcceptedOrdersTotal += row.OrderTotal;
                        model.PrintedOrdersCount = row.OrderCount;
                        break;
                    case OrderStatuses.Shipped:
                        model.AcceptedOrdersTotal += row.OrderTotal;
                        model.ShippedOrdersCount = row.OrderCount;
                        break;

                    case OrderStatuses.PendingInventory:
                    default:
                        break;
                }
            }



            // Return the data
            return new JsonNetResult(new 
            {
                data = model
            });
        }

        [HttpPost]
        public JsonNetResult GetEnrollmentsByType()
        {
            var months = 3;

            // Get the data
            dynamic data;
            using (var context = Exigo.Sql())
            {
                data = context.Query(@"
                -- Enrollments By Type
                SELECT c.CustomerTypeID
	                 , ct.CustomerTypeDescription
	                 , ""Month"" = month(c.CreatedDate)
	                 , ""Year"" = year(c.CreatedDate)
	                 , CustomerCount = count(*)
                FROM
	                Customers c
	                INNER JOIN CustomerTypes ct
		                ON ct.CustomerTypeID = c.CustomerTypeID
                WHERE
	                c.CreatedDate >= @startdate
	                AND c.CreatedDate < @enddate
                    AND c.CustomerTypeID IN @customertypes
                    AND c.MainCountry in @countries
                GROUP BY
	                c.CustomerTypeID
                  , ct.CustomerTypeDescription
                  , month(c.CreatedDate)
                  , year(c.CreatedDate)
                ORDER BY
	                c.CustomerTypeID
                  , year(c.CreatedDate)
                  , month(c.CreatedDate)
                OPTION(MAXDOP 8)
            ", new
             {
                 startdate = DateTime.Now.AddMonths(-months).BeginningOfMonth(),
                 enddate = DateTime.Now.EndOfDay(),
                 customertypes = new List<int>
                 {
                     CustomerTypes.RetailCustomer,
                     CustomerTypes.Distributor
                 },
                 countries = CountryCodes.All
             }).ToList();
            }


            // Create the data source
            var dataSource = new MultiSeriesChartXMLDataSource();


            // Customize our chart
            dataSource.yAxisName = "Enrollments";
            dataSource.ShowValues = true;


            // Add our categories and datasets
            foreach (var row in data)
            {
                dataSource.AddCategory(new DateTime(row.Year, row.Month, 1).ToString("MMMM yyyy"));
                dataSource.AddSeries(row.CustomerTypeDescription, row.CustomerCount);
            }


            // Return the data
            return new JsonNetResult(new
            {
                xml = dataSource.ToString()
            });
        }

        [HttpPost]
        public JsonNetResult GetRevenueByMonth()
        {
            var months = 4;

            // Get the data
            dynamic data;
            using (var context = Exigo.Sql())
            {
                data = context.Query(@"
                -- Revenue by Month
                SELECT ""Month"" = month(o.OrderDate)
	                    , ""Year"" = year(o.OrderDate)
	                    , Revenue = sum(o.Total)
                FROM
	                Orders o
                WHERE
	                o.OrderDate >= @startdate
	                AND o.OrderDate < @enddate
                    AND o.Country in @countries
                GROUP BY
	                month(o.OrderDate)
                    , year(o.OrderDate)
                ORDER BY
	                year(o.OrderDate)
                    , month(o.OrderDate)
                OPTION(MAXDOP 8)
            ", new
             {
                 startdate = DateTime.Now.AddMonths(-months).BeginningOfMonth(),
                 enddate   = DateTime.Now.EndOfDay(),
                 countries = CountryCodes.All
             }).ToList();
            }


            // Create the data source
            var dataSource = new SingleSeriesChartXMLDataSource();


            // Customize our chart
            dataSource.yAxisName = "Revenue";
            dataSource.ShowValues = true;
            dataSource.NumberPrefix = "$";
            dataSource.DecimalPlaces = 2;


            // Add our categories and datasets
            foreach (var row in data)
            {
                var label = DateTime.Now.BeginningOfMonth().ToString("MMMM yyyy");
                var value = Convert.ToDecimal(row.Revenue);

                dataSource.AddSeries(label, value);
            }


            // Return the data
            return new JsonNetResult(new
            {
                xml = dataSource.ToString()
            });
        }

        [HttpPost]
        public JsonNetResult GetRevenueByTopProducts()
        {
            // Get the data
            dynamic data;
            using (var context = Exigo.Sql())
            {
                data = context.Query(@"
                -- Top 10 Products by Revenue
                SELECT TOP 10 od.ItemID
			                , i.ItemDescription
			                , i.ItemCode
			                , Revenue = sum(o.Total)
                FROM
	                Orders o
	                INNER JOIN OrderDetails od
		                ON od.OrderID = o.OrderID
	                INNER JOIN Items i
		                ON i.ItemID = od.ItemID
                WHERE
	                o.OrderDate >= @startdate
	                AND o.OrderDate < @enddate
                    AND o.Country in @countries
                GROUP BY
	                od.ItemID
                  , i.ItemDescription
                  , i.ItemCode
                ORDER BY
	                sum(o.Total) DESC
                OPTION(MAXDOP 8)
            ", new
             {
                 startdate = DateTime.Now.AddMonths(-1).BeginningOfMonth(),
                 enddate   = DateTime.Now.BeginningOfMonth(),
                 countries = CountryCodes.All
             }).ToList();
            }


            // Create the data source
            var dataSource = new PieChartXMLDataSource();


            // Customize our chart
            dataSource.ShowValues = true;
            dataSource.ShowLabels = false;
            dataSource.ShowPercentValues = false;
            dataSource.NumberPrefix = "$";
            dataSource.DecimalPlaces = 2;


            // Add our categories and datasets
            foreach (var row in data)
            {
                var label = row.ItemDescription;
                var value = row.Revenue;

                dataSource.AddSeries(label, value);
            }


            // Return the data
            return new JsonNetResult(new
            {
                xml = dataSource.ToString()
            });
        }

        [HttpPost]
        public JsonNetResult GetSalesBySource()
        {
            // Get the data
            dynamic data;
            using (var context = Exigo.Sql())
            {
                data = context.Query(@"
                --Sales by Source
                SELECT o.OrderTypeID
	                 , ot.OrderTypeDescription
	                 , Revenue = sum(o.Total)
                FROM
	                Orders o
	                INNER JOIN OrderTypes ot
		                ON ot.OrderTypeID = o.OrderTypeID
                WHERE
	                o.OrderDate >= @startdate
	                AND o.OrderDate < @enddate
                    AND o.Country in @countries
                GROUP BY
	                o.OrderTypeID
                  , ot.OrderTypeDescription
                ORDER BY
	                sum(o.Total) DESC
                OPTION(MAXDOP 8)
            ", new
             {
                 startdate = DateTime.Now.AddMonths(-1).BeginningOfMonth(),
                 enddate   = DateTime.Now.BeginningOfMonth(),
                 countries = CountryCodes.All
             }).ToList();
            }


            // Create the data source
            var dataSource = new PieChartXMLDataSource();


            // Customize our chart
            dataSource.ShowValues        = true;
            dataSource.ShowLabels        = false;
            dataSource.ShowPercentValues = false;
            dataSource.NumberPrefix      = "$";
            dataSource.DecimalPlaces     = 2;


            // Add our categories and datasets
            foreach (var row in data)
            {
                var label = row.OrderTypeDescription;
                var value = row.Revenue;

                dataSource.AddSeries(label, value);
            }


            // Return the data
            return new JsonNetResult(new
            {
                xml = dataSource.ToString()
            });
        }
    }
}
