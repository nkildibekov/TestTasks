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
    [RoutePrefix("reports")]
    [Route("{action=summaryreport}")]
    public class ReportingController : Controller
    {
        private static readonly string[] Field4TestAccountIDs = new string[] { "6500000009", "2928284" };

        [Route("summary")]
        public ActionResult SummaryReport()
        {
            var model = new SummaryReportFiltersViewModel();

            model.StartDate         = DateTime.Now.BeginningOfDay().AddDays(-30);
            model.EndDate           = DateTime.Now.EndOfDay();
            model.ReportType        = SummaryReportType.TransactionCounts;
            model.OrderStatusesType = SummaryReportOrderStatusesType.All;
            model.PaymentTypes      = new List<SummaryReportPaymentType>();
            model.RevenueType       = SummaryReportRevenueType.Total;


            if (Request.IsAjaxRequest()) return PartialView(model);
            else return View(model);
        }

        [HttpPost]
        public ActionResult IBOEnrollment(SummaryReportFiltersViewModel filters)
        {
            var viewModel = new SummaryReportResultViewModel();
            viewModel.Settings = GetReportSettings(filters);
            
            using (var context = Exigo.Sql())
            {
                var data = context.QueryMultiple(@"
                    -- IBO Enrollments
                    SELECT Description = case 
			                    when o.ordertypeid = 11 then 'Online'
			                    else 'Paper - Keyed by Customer Service'
		                        end,
		                    Value = case 
                                        when @reporttype = 1 then COUNT(o.OrderID)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(o.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(o.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(o.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(o.ShippingTotal)
                                        else -1
                                    end,
                           [Key] = o.ordertypeid,
	                       Month = month(o.orderdate),
	                       Day = day(o.orderdate),
	                       Year = year(o.orderdate)
                    FROM   orders o 
                           LEFT JOIN orderdetails od 
                                  ON o.orderid = od.orderid 
                           LEFT JOIN customers c 
                                  ON o.customerid = c.customerid                            
                           " + ((filters.PaymentTypes.Count > 0) ? "INNER JOIN Payments p ON p.OrderID = o.OrderID AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" : "") + @"
                    WHERE  od.itemcode = '401' 
                           AND o.ordertypeid NOT IN ( 4, 5, 6, 7, 8, 10, 12, 13 ) 
                           AND c.field4 NOT IN @testaccountids
                           AND o.orderdate BETWEEN @startdate AND @enddate
                           AND    ((@orderstatusestype = 1 AND o.OrderStatusID in (1, 2, 3, 4, 5, 6, 7, 8, 9))
                                OR (@orderstatusestype = 2 AND o.OrderStatusID in (7, 8, 9))
                                OR (@orderstatusestype = 3 AND o.OrderStatusID in (1, 2, 3, 5, 6))
                                OR (@orderstatusestype = 4 AND o.OrderStatusID in (4)))
                    GROUP  BY o.ordertypeid, day(o.orderdate), month(o.orderdate), year(o.orderdate)
                    ORDER BY year(o.orderdate), month(o.orderdate), day(o.orderdate)


                    -- Enrollment Type
                    SELECT Value = case 
                                        when @reporttype = 1 then COUNT(c.CustomerID)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(o.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(o.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(o.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(o.ShippingTotal)
                                        else -1
                                    end, 
                           [Key] = c.field3,
	                       Description = 
		                    case
			                    when c.field3 = 1 then 'Individuals'
			                    when c.field3 = 2 then 'Individual w\Business Interest'
			                    when c.field3 = 3 then 'Business'
                                else c.field3
		                    end,
                            Month = month(o.orderdate),
	                       Day = day(o.orderdate),
	                       Year = year(o.orderdate)
                    FROM   orders o 
                           LEFT JOIN orderdetails od 
                                  ON o.orderid = od.orderid 
                           LEFT JOIN customers c 
                                  ON o.customerid = c.customerid 
                            " + ((filters.PaymentTypes.Count > 0) ? "INNER JOIN Payments p ON p.OrderID = o.OrderID AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" : "") + @"
                    WHERE  od.itemcode = '401' 
                           AND o.ordertypeid NOT IN ( 4, 5, 6, 7, 8, 10, 12, 13 ) 
                           AND c.field4 NOT IN @testaccountids
                           AND c.maincountry = 'MX' 
                           AND o.orderdate BETWEEN @startdate AND @enddate 
                           AND    ((@orderstatusestype = 1 AND o.OrderStatusID in (1, 2, 3, 4, 5, 6, 7, 8, 9))
                                OR (@orderstatusestype = 2 AND o.OrderStatusID in (7, 8, 9))
                                OR (@orderstatusestype = 3 AND o.OrderStatusID in (1, 2, 3, 5, 6))
                                OR (@orderstatusestype = 4 AND o.OrderStatusID in (4)))
                    GROUP  BY c.field3, day(o.orderdate), month(o.orderdate), year(o.orderdate)
                    ORDER BY year(o.orderdate), month(o.orderdate), day(o.orderdate)


                    -- Enrollment Kit Only
                    SELECT Description = 
		                    case 
			                    when p.PaymentTypeID is null then 'Unpaid'
                                when p.PaymentTypeID = 14 then 'Cash (Banamex)'
                                when p.PaymentTypeID = 15 then 'Cash (Bancomer)'
                                when p.PaymentTypeID = 16 then 'Bank Transfer'
			                    else pt.PaymentTypeDescription
		                    end,
		                    p.PaymentTypeID,
		                    Value = case 
                                        when @reporttype = 1 then COUNT(od.ItemCode)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(o.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(o.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(o.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(o.ShippingTotal)
                                        else -1
                                    end, 
		                    Month = month(o.orderdate),
	                        Day = day(o.orderdate),
	                        Year = year(o.orderdate)
                    FROM   orders o 
                            LEFT JOIN orderdetails od 
                                    ON o.orderid = od.orderid 
                            LEFT JOIN customers c 
                                    ON o.customerid = c.customerid 
                            LEFT JOIN (SELECT Max(orderline) AS maxod, 
                                                orderid 
                                        FROM   orderdetails 
                                        GROUP  BY orderid) od2 
                                    ON o.orderid = od2.orderid 
                            " + ((filters.PaymentTypes.Count > 0) 
                                ? "INNER JOIN Payments p ON p.OrderID = o.OrderID AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" 
                                : "LEFT JOIN Payments p on p.OrderID = o.OrderID") + @"
		                    left join PaymentTypes pt
			                    on pt.PaymentTypeID = p.PaymentTypeID
                    WHERE  od2.maxod = 1 
                            AND od.itemcode = '401' 
                            AND o.ordertypeid NOT IN ( 4, 5, 6, 7, 8, 10, 12, 13 ) 
                            AND c.field4 NOT IN @testaccountids
                            AND o.orderdate BETWEEN @startdate AND @enddate 
                            AND   ((@orderstatusestype = 1 AND o.OrderStatusID in (1, 2, 3, 4, 5, 6, 7, 8, 9))
                                OR (@orderstatusestype = 2 AND o.OrderStatusID in (7, 8, 9))
                                OR (@orderstatusestype = 3 AND o.OrderStatusID in (1, 2, 3, 5, 6))
                                OR (@orderstatusestype = 4 AND o.OrderStatusID in (4)))
                    GROUP  BY od.itemdescription, p.PaymentTypeID, pt.PaymentTypeDescription, day(o.orderdate), month(o.orderdate), year(o.orderdate)
                    ORDER BY year(o.orderdate), month(o.orderdate), day(o.orderdate)


                    -- Enrollment Bundle Types
                    SELECT Description = od.itemdescription, 
		                    Value = case 
                                        when @reporttype = 1 then COUNT(od.ItemCode)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(o.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(o.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(o.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(o.ShippingTotal)
                                        else -1
                                    end,
		                    Month = month(o.orderdate),
	                        Day = day(o.orderdate),
	                        Year = year(o.orderdate)
       
                    FROM   orders o 
                           LEFT JOIN orderdetails od 
                                  ON o.orderid = od.orderid 
                           LEFT JOIN customers c 
                                  ON o.customerid = c.customerid 
                           LEFT JOIN (SELECT Max(orderline) AS maxod, 
                                             orderid 
                                      FROM   orderdetails 
                                      GROUP  BY orderid) od2 
                                  ON o.orderid = od2.orderid 
                           LEFT JOIN items i 
                                  ON od.itemid = i.itemid 
                            " + ((filters.PaymentTypes.Count > 0) ? "INNER JOIN Payments p ON p.OrderID = o.OrderID AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" : "") + @"
                    WHERE  i.othercheck4 = 1 
                           AND o.ordertypeid NOT IN ( 4, 5, 6, 7, 8, 10, 12, 13 ) 
                           AND c.field4 NOT IN @testaccountids
                           AND o.orderdate BETWEEN @startdate AND @enddate 
                           AND    ((@orderstatusestype = 1 AND o.OrderStatusID in (1, 2, 3, 4, 5, 6, 7, 8, 9))
                                OR (@orderstatusestype = 2 AND o.OrderStatusID in (7, 8, 9))
                                OR (@orderstatusestype = 3 AND o.OrderStatusID in (1, 2, 3, 5, 6))
                                OR (@orderstatusestype = 4 AND o.OrderStatusID in (4)))
                    GROUP  BY od.itemdescription, day(o.orderdate), month(o.orderdate), year(o.orderdate)
                    ORDER BY year(o.orderdate), month(o.orderdate), day(o.orderdate)
                ", new
                 {
                     startdate             = filters.StartDate.BeginningOfDay(),
                     enddate               = filters.EndDate.EndOfDay(),
                     testaccountids        = Field4TestAccountIDs,

                     reporttype            = filters.ReportType,
                     orderstatusestype     = filters.OrderStatusesType,
                     paymenttypes          = filters.PaymentTypeIDs,
                     revenuetype           = filters.RevenueType
                 });

                viewModel.Groups.Add(new SummaryReportRowGroup("Totals") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("Enrollment Type") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("Enrollment Kit Only") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("Enrollment Bundle Types") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
            }


            if (Request.IsAjaxRequest()) return PartialView("_ReportTable", viewModel);
            else return View("_ReportTable", viewModel);
        }

        [HttpPost]
        public ActionResult AOPEnrollmentActivity(SummaryReportFiltersViewModel filters)
        {
            var viewModel = new SummaryReportResultViewModel();
            viewModel.Settings = GetReportSettings(filters);

            using (var context = Exigo.Sql())
            {
                var data = context.QueryMultiple(@"
                    -- IBO AOP During Enrollment
                    SELECT Description = 
		                    case
			                    when a.AutoOrderPaymentTypeID in (1,2) then 'Credit Card'
			                    when a.AutoOrderPaymentTypeID = 4 then 'Cash'
			                    else pt.AutoOrderPaymentTypeDescription
		                    end,
		                    [Key] = c.customertypeid,
		                    Value = case 
                                        when @reporttype = 1 then COUNT(c.CustomerID)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(a.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(a.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(a.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(a.ShippingTotal)
                                        else -1
                                    end, 
	                       Month = month(a.createddate),
	                        Day = day(a.createddate),
	                        Year = year(a.createddate)
                    FROM   customers c 
                           LEFT JOIN autoorders a 
                                  ON c.customerid = a.customerid 
		                    left join AutoOrderPaymentTypes pt
			                    on pt.AutoOrderPaymentTypeID = a.AutoOrderPaymentTypeID
                    WHERE  a.autoorderstatusid = 0
                           AND c.CustomerTypeID in (" + string.Join(",", new int[] { CustomerTypes.Distributor }) + @")
                           AND c.field4 NOT IN @testaccountids
                           AND c.maincountry = 'MX' 
                           AND a.createddate BETWEEN @startdate AND @enddate 
                           AND dateadd(month, datediff(month, 0, a.CreatedDate),0) = dateadd(month, datediff(month, 0, c.CreatedDate),0)
                           " + ((filters.AutoOrderPaymentTypeIDs.Count > 0) ? "AND a.AutoOrderPaymentTypeID IN (" + string.Join(",", filters.AutoOrderPaymentTypeIDs) + @")" : "") + @"
                    GROUP  BY c.customertypeid, a.AutoOrderPaymentTypeID, pt.AutoOrderPaymentTypeDescription, day(a.createddate), month(a.createddate), year(a.createddate)
                    ORDER BY year(a.createddate), month(a.createddate), day(a.createddate)


                    -- IBO AOP Created After Enrollment
                    SELECT Description = 
		                    case
			                    when a.AutoOrderPaymentTypeID in (1,2) then 'Credit Card'
			                    when a.AutoOrderPaymentTypeID = 4 then 'Cash'
			                    else pt.AutoOrderPaymentTypeDescription
		                    end,
		                    [Key] = c.customertypeid,
		                    Value = case 
                                        when @reporttype = 1 then COUNT(c.CustomerID)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(a.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(a.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(a.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(a.ShippingTotal)
                                        else -1
                                    end, 
	                       Month = month(a.createddate),
	                        Day = day(a.createddate),
	                        Year = year(a.createddate)
                    FROM   customers c 
                           LEFT JOIN autoorders a 
                                  ON c.customerid = a.customerid 
		                    left join AutoOrderPaymentTypes pt
			                    on pt.AutoOrderPaymentTypeID = a.AutoOrderPaymentTypeID
                    WHERE  a.autoorderstatusid = 0
                           AND c.CustomerTypeID = (" + string.Join(",", new int[] { CustomerTypes.Distributor }) + @")
                           AND c.field4 NOT IN @testaccountids
                           AND c.maincountry = 'MX' 
                           AND a.createddate BETWEEN @startdate AND @enddate 
                           AND dateadd(month, datediff(month, 0, a.CreatedDate),0) <> dateadd(month, datediff(month, 0, c.CreatedDate),0)
                           " + ((filters.AutoOrderPaymentTypeIDs.Count > 0) ? "AND a.AutoOrderPaymentTypeID IN (" + string.Join(",", filters.AutoOrderPaymentTypeIDs) + @")" : "") + @"
                    GROUP  BY c.customertypeid, a.AutoOrderPaymentTypeID, pt.AutoOrderPaymentTypeDescription, day(a.createddate), month(a.createddate), year(a.createddate)
                    ORDER BY year(a.createddate), month(a.createddate), day(a.createddate)


                    -- Retail AOP
                    SELECT Description = 
		                    case
			                    when a.AutoOrderPaymentTypeID in (1,2) then 'Credit Card'
			                    when a.AutoOrderPaymentTypeID = 4 then 'Cash'
			                    else pt.AutoOrderPaymentTypeDescription
		                    end,
		                    [Key] = c.customertypeid,
		                    Value = case 
                                        when @reporttype = 1 then COUNT(c.CustomerID)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(a.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(a.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(a.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(a.ShippingTotal)
                                        else -1
                                    end, 
	                       Month = month(a.createddate),
	                        Day = day(a.createddate),
	                        Year = year(a.createddate)
                    FROM   customers c 
                           LEFT JOIN autoorders a 
                                  ON c.customerid = a.customerid 
		                    left join AutoOrderPaymentTypes pt
			                    on pt.AutoOrderPaymentTypeID = a.AutoOrderPaymentTypeID
                    WHERE  a.autoorderstatusid = 0
	                       AND c.CustomerTypeID = (" + string.Join(",", new int[] { CustomerTypes.RetailCustomer }) + @")
                           AND c.field4 NOT IN @testaccountids
                           AND c.maincountry = 'MX' 
                           AND a.createddate BETWEEN @startdate AND @enddate 
                           " + ((filters.AutoOrderPaymentTypeIDs.Count > 0) ? "AND a.AutoOrderPaymentTypeID IN (" + string.Join(",", filters.AutoOrderPaymentTypeIDs) + @")" : "") + @"
                    GROUP  BY c.customertypeid, a.AutoOrderPaymentTypeID, pt.AutoOrderPaymentTypeDescription, day(a.createddate), month(a.createddate), year(a.createddate)
                    ORDER BY year(a.createddate), month(a.createddate), day(a.createddate)

                ", new
                 {
                     startdate         = filters.StartDate.BeginningOfDay(),
                     enddate           = filters.EndDate.EndOfDay(),
                     testaccountids    = Field4TestAccountIDs,

                     reporttype        = filters.ReportType,
                     orderstatusestype = filters.OrderStatusesType,
                     paymenttypes      = filters.PaymentTypeIDs,
                     revenuetype       = filters.RevenueType
                 });

                viewModel.Groups.Add(new SummaryReportRowGroup("IBO AOP During Enrollment") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("IBO AOP Added After Enrollment") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("Retail AOP") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
            }


            if (Request.IsAjaxRequest()) return PartialView("_ReportTable", viewModel);
            else return View("_ReportTable", viewModel);
        }

        [HttpPost]
        public ActionResult AOPForecast(SummaryReportFiltersViewModel filters)
        {
            var viewModel = new SummaryReportResultViewModel();
            viewModel.Settings = GetReportSettings(filters);

            filters.StartDate = DateTime.Now.BeginningOfDay();
            filters.EndDate = filters.StartDate.AddDays(30).EndOfDay();

            using (var context = Exigo.Sql())
            {
                var data = context.QueryMultiple(@"
                    -- Retail AOP Forecast
                    SELECT Description = 
		                    case
			                    when a.AutoOrderPaymentTypeID in (1,2) then 'Credit Card'
			                    when a.AutoOrderPaymentTypeID = 4 then 'Cash'
			                    else pt.AutoOrderPaymentTypeDescription
		                    end,
		                    Value = case 
                                        when @reporttype = 1 then COUNT(aos.AutoOrderID)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(a.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(a.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(a.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(a.ShippingTotal)
					                    else -1
                                    end, 
                            [Key] = c.CustomerTypeID,
	                        Month = month(aos.ScheduledDate),
	                        Day = day(aos.ScheduledDate),
	                        Year = year(aos.ScheduledDate)
                    from AutoOrderSchedules aos
		                    inner join AutoOrders a
			                    on a.AutoOrderID = aos.AutoOrderID
		                    left join AutoOrderPaymentTypes pt
			                    on pt.AutoOrderPaymentTypeID = a.AutoOrderPaymentTypeID
                            LEFT JOIN customers c 
                                ON a.customerid = c.customerid 
                    WHERE  a.autoorderstatusid = 0
		                    AND c.CustomerTypeID in (" + CustomerTypes.RetailCustomer + @")
                            AND c.field4 NOT IN @testaccountids
		                    AND c.maincountry = 'MX' 
                            AND aos.IsEnabled = 1
		                    AND aos.ScheduledDate between @startdate and @enddate
                            " + ((filters.AutoOrderPaymentTypeIDs.Count > 0) ? "AND a.AutoOrderPaymentTypeID IN (" + string.Join(",", filters.AutoOrderPaymentTypeIDs) + @")" : "") + @"
                    GROUP  BY c.CustomerTypeID, a.AutoOrderPaymentTypeID, pt.AutoOrderPaymentTypeDescription, day(aos.ScheduledDate), month(aos.ScheduledDate), year(aos.ScheduledDate)
                    ORDER BY year(aos.ScheduledDate), month(aos.ScheduledDate), day(aos.ScheduledDate)



                    -- IBO AOP Forecast
                    SELECT Description = 
		                    case
			                    when a.AutoOrderPaymentTypeID in (1,2) then 'Credit Card'
			                    when a.AutoOrderPaymentTypeID = 4 then 'Cash'
			                    else pt.AutoOrderPaymentTypeDescription
		                    end,
		                    Value = case 
                                        when @reporttype = 1 then COUNT(aos.AutoOrderID)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(a.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(a.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(a.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(a.ShippingTotal)
					                    else -1
                                    end, 
                            [Key] = c.CustomerTypeID,
	                        Month = month(aos.ScheduledDate),
	                        Day = day(aos.ScheduledDate),
	                        Year = year(aos.ScheduledDate)
                    from AutoOrderSchedules aos
		                    inner join AutoOrders a
			                    on a.AutoOrderID = aos.AutoOrderID
		                    left join AutoOrderPaymentTypes pt
			                    on pt.AutoOrderPaymentTypeID = a.AutoOrderPaymentTypeID
                            LEFT JOIN customers c 
                                ON a.customerid = c.customerid 
                    WHERE  a.autoorderstatusid = 0
		                    AND c.CustomerTypeID in (" + CustomerTypes.Distributor + @")
                            AND c.field4 NOT IN @testaccountids
		                    AND c.maincountry = 'MX' 
                            AND aos.IsEnabled = 1
		                    AND aos.ScheduledDate between @startdate and @enddate
                            " + ((filters.AutoOrderPaymentTypeIDs.Count > 0) ? "AND a.AutoOrderPaymentTypeID IN (" + string.Join(",", filters.AutoOrderPaymentTypeIDs) + @")" : "") + @"
                    GROUP  BY c.CustomerTypeID, a.AutoOrderPaymentTypeID, pt.AutoOrderPaymentTypeDescription, day(aos.ScheduledDate), month(aos.ScheduledDate), year(aos.ScheduledDate)
                    ORDER BY year(aos.ScheduledDate), month(aos.ScheduledDate), day(aos.ScheduledDate)

                ", new
                 {
                     startdate         = filters.StartDate,
                     enddate           = filters.EndDate,
                     testaccountids    = Field4TestAccountIDs,

                     reporttype        = filters.ReportType,
                     orderstatusestype = filters.OrderStatusesType,
                     paymenttypes      = filters.PaymentTypeIDs,
                     revenuetype       = filters.RevenueType
                 });

                viewModel.Groups.Add(new SummaryReportRowGroup("Retail AOP ({0:M/d} - {1:M/d})".FormatWith(filters.StartDate, filters.EndDate)) { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("IBO AOP ({0:M/d} - {1:M/d})".FormatWith(filters.StartDate, filters.EndDate)) { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
            }


            if (Request.IsAjaxRequest()) return PartialView("_ReportTable", viewModel);
            else return View("_ReportTable", viewModel);
        }

        [HttpPost]
        public ActionResult ProductOrders(SummaryReportFiltersViewModel filters)
        {
            var viewModel = new SummaryReportResultViewModel();
            viewModel.Settings = GetReportSettings(filters);

            using (var context = Exigo.Sql())
            {
                var records = context.Query<SummaryReportRecord>(@"
                    -- Product Orders
                    SELECT Description = 
		                    case 
			                    when p.PaymentTypeID is null then 'Unpaid'
                                when p.PaymentTypeID = 14 then 'Cash (Banamex)'
                                when p.PaymentTypeID = 15 then 'Cash (Bancomer)'
			                    else pt.PaymentTypeDescription
		                    end,
		                    Value = case 
                                        when @reporttype = 1 then COUNT(o.PriceTypeID)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(o.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(o.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(o.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(o.ShippingTotal)
										else -1
                                    end, 
                           [Key] = o.pricetypeid,
	                       Month = month(o.orderdate),
	                        Day = day(o.orderdate),
	                        Year = year(o.orderdate)
                    FROM   orders o 
                           LEFT JOIN customers c 
                                  ON o.customerid = c.customerid 
                            " + ((filters.PaymentTypes.Count > 0) 
                                  ? "INNER JOIN Payments p ON p.OrderID = o.OrderID AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" 
                                  : "LEFT JOIN Payments p on p.OrderID = o.OrderID") + @"		                    
		                    left join PaymentTypes pt
			                    on pt.PaymentTypeID = p.PaymentTypeID
                    WHERE  o.ordertypeid NOT IN ( 5, 6, 7, 8, 10, 12, 13 ) 
	                       and o.PriceTypeID in (" + string.Join(",", new int[] { PriceTypes.Retail, PriceTypes.Preferred, PriceTypes.Wholesale }) + @")
                           AND c.field4 NOT IN @testaccountids 
                           AND o.orderdate BETWEEN @startdate AND @enddate 
                           AND    ((@orderstatusestype = 1 AND o.OrderStatusID in (1, 2, 3, 4, 5, 6, 7, 8, 9))
                                OR (@orderstatusestype = 2 AND o.OrderStatusID in (7, 8, 9))
                                OR (@orderstatusestype = 3 AND o.OrderStatusID in (1, 2, 3, 5, 6))
                                OR (@orderstatusestype = 4 AND o.OrderStatusID in (4)))
                    GROUP  BY o.pricetypeid, p.PaymentTypeID, pt.PaymentTypeDescription, day(o.orderdate), month(o.orderdate), year(o.orderdate)
                    ORDER BY year(o.orderdate), month(o.orderdate), day(o.orderdate)
                ", new
                 {
                     startdate         = filters.StartDate.BeginningOfDay(),
                     enddate           = filters.EndDate.EndOfDay(),
                     testaccountids    = Field4TestAccountIDs,

                     reporttype        = filters.ReportType,
                     orderstatusestype = filters.OrderStatusesType,
                     paymenttypes      = filters.PaymentTypeIDs,
                     revenuetype       = filters.RevenueType
                 }).ToList();

                viewModel.Groups.Add(new SummaryReportRowGroup("Retail") { Rows = GetSummaryReportRows(records.Where(c => (int)c.Key == PriceTypes.Retail).ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("Preferred") { Rows = GetSummaryReportRows(records.Where(c => (int)c.Key == PriceTypes.Preferred).ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("Wholesale") { Rows = GetSummaryReportRows(records.Where(c => (int)c.Key == PriceTypes.Wholesale).ToList(), filters) });
            }


            if (Request.IsAjaxRequest()) return PartialView("_ReportTable", viewModel);
            else return View("_ReportTable", viewModel);
        }

        [HttpPost]
        public ActionResult PaymentMethods(SummaryReportFiltersViewModel filters)
        {
            var viewModel = new SummaryReportResultViewModel();
            viewModel.Settings = GetReportSettings(filters);

            using (var context = Exigo.Sql())
            {
                var records = context.Query<SummaryReportRecord>(@"
                    -- Payment Methods
                    SELECT Description = 
		                    case 
                                when p.PaymentTypeID = 0 then 'Cash at the Office'
                                when p.PaymentTypeID = 6 then 'Used Credit'
			                    when p.PaymentTypeID = 14 then 'Cash (Banamex)'
			                    when p.PaymentTypeID = 15 then 'Cash (Bancomer)'
                                when p.PaymentTypeID = 16 then 'Bank Transfer'
			                    else pt.PaymentTypeDescription
		                    end,
		                    Value = case 
                                        when @reporttype = 1 then COUNT(p.PaymentID)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(p.Amount)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(p.Amount)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(p.Amount)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(p.Amount)
										else -1
                                    end, 
                           [Key] = p.paymenttypeid,
	                       Month = month(p.PaymentDate),
	                        Day = day(p.PaymentDate),
	                        Year = year(p.PaymentDate)
                    FROM   payments p 
		                    left join PaymentTypes pt
			                    on pt.PaymentTypeID = p.PaymentTypeID
                           LEFT JOIN customers c 
                                  ON p.customerid = c.customerid 
                           LEFT JOIN orders o 
                                  ON o.orderid = p.orderid 
                    WHERE  o.ordertypeid NOT IN ( 5, 6, 7, 8, 10, 12, 13 ) 
                           AND c.field4 NOT IN @testaccountids
                           AND o.orderdate BETWEEN @startdate AND @enddate 
                            " + ((filters.PaymentTypes.Count > 0) ? "AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" : "") + @"
                    GROUP  BY p.paymenttypeid, p.PaymentTypeID, pt.PaymentTypeDescription, day(p.PaymentDate), month(p.PaymentDate), year(p.PaymentDate)
                    ORDER BY year(p.PaymentDate), month(p.PaymentDate), day(p.PaymentDate)
                ", new
                 {
                     startdate         = filters.StartDate.BeginningOfDay(),
                     enddate           = filters.EndDate.EndOfDay(),
                     testaccountids    = Field4TestAccountIDs,

                     reporttype        = filters.ReportType,
                     orderstatusestype = filters.OrderStatusesType,
                     paymenttypes      = filters.PaymentTypeIDs,
                     revenuetype       = filters.RevenueType
                 }).ToList();

                viewModel.Groups.Add(new SummaryReportRowGroup("Payment Methods") { Rows = GetSummaryReportRows(records, filters) });
            }



            if (Request.IsAjaxRequest()) return PartialView("_ReportTable", viewModel);
            else return View("_ReportTable", viewModel);
        }

        [HttpPost]
        public ActionResult Shipping(SummaryReportFiltersViewModel filters)
        {
            var viewModel = new SummaryReportResultViewModel();
            viewModel.Settings = GetReportSettings(filters);

            using (var context = Exigo.Sql())
            {
                var data = context.QueryMultiple(@"
                    -- Total Orders Shipped
                    SELECT Description = sm.ShipMethodDescription,
		                        Value = case 
                                        when @reporttype = 1 then COUNT(o.ShipMethodID)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(o.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(o.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(o.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(o.ShippingTotal)
										else -1
                                    end, 
                               [Key] = o.shipmethodid,
	                           Month = month(o.orderdate),
	                            Day = day(o.orderdate),
	                            Year = year(o.orderdate)
                        FROM   orders o 
                               LEFT JOIN customers c 
                                      ON o.customerid = c.customerid 
		                        left join ShipMethods sm
			                        on sm.ShipMethodID = o.ShipMethodID
                                " + ((filters.PaymentTypes.Count > 0) ? "INNER JOIN Payments p ON p.OrderID = o.OrderID AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" : "") + @"
                        WHERE  o.ordertypeid NOT IN (" + string.Join(",", new int[] { 
                                                         OrderTypes.Import, 
                                                         OrderTypes.BackOrder, 
                                                         OrderTypes.ReturnOrder,
                                                         OrderTypes.TicketSystem,
                                                         OrderTypes.BackOrderParentNoShip,
                                                         OrderTypes.ChildOrder }) + @")
                           AND o.OrderStatusID in (" + string.Join(",", new int[] { OrderStatuses.Shipped }) + @")
                           AND c.field4 NOT IN @testaccountids 
                           AND o.orderdate BETWEEN @startdate AND @enddate
                    GROUP  BY o.shipmethodid, sm.ShipMethodDescription, day(o.orderdate), month(o.orderdate), year(o.orderdate)
                    ORDER BY year(o.orderdate), month(o.orderdate), day(o.orderdate)



                    -- Pending Orders
                    SELECT Description = 'Orders Pending',
		                    Value = case 
                                        when @reporttype = 1 then COUNT(o.OrderStatusID)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(o.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(o.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(o.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(o.ShippingTotal)
										else -1
                                    end, 
                           [Key] = o.orderstatusid,
	                       Month = month(o.orderdate),
	                        Day = day(o.orderdate),
	                        Year = year(o.orderdate)
                    FROM   orders o 
                           LEFT JOIN customers c 
                                  ON o.customerid = c.customerid 
                            " + ((filters.PaymentTypes.Count > 0) ? "INNER JOIN Payments p ON p.OrderID = o.OrderID AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" : "") + @"
                    WHERE  o.ordertypeid NOT IN (" + string.Join(",", new int[] { 
                                                         OrderTypes.Import, 
                                                         OrderTypes.BackOrder, 
                                                         OrderTypes.ReturnOrder,
                                                         OrderTypes.TicketSystem,
                                                         OrderTypes.BackOrderParentNoShip,
                                                         OrderTypes.ChildOrder }) + @")
	                       AND o.OrderStatusID in (" + string.Join(",", new int[] { 
                                                         OrderStatuses.Pending, 
                                                         OrderStatuses.CCDeclined, 
                                                         OrderStatuses.ACHDeclined,
                                                         OrderStatuses.CCPending,
                                                         OrderStatuses.ACHPending }) + @")
                           AND c.field4 NOT IN @testaccountids
                           AND o.orderdate BETWEEN @startdate AND @enddate 
                    GROUP  BY o.orderstatusid, day(o.orderdate), month(o.orderdate), year(o.orderdate)
                    ORDER BY year(o.orderdate), month(o.orderdate), day(o.orderdate)



                    -- Return Orders
                    SELECT Description = 'Returns', 
		                    Value = case 
                                        when @reporttype = 1 then COUNT(o.OrderTypeID)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(o.Total)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(o.Subtotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(o.TaxTotal)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(o.ShippingTotal)
										else -1
                                    end, 
                           [Key] = o.ordertypeid,
	                       Month = month(o.orderdate),
	                        Day = day(o.orderdate),
	                        Year = year(o.orderdate)
                    FROM   orders o 
                           LEFT JOIN customers c 
                                  ON o.customerid = c.customerid 
                            " + ((filters.PaymentTypes.Count > 0) ? "INNER JOIN Payments p ON p.OrderID = o.OrderID AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" : "") + @"
                    WHERE  o.ordertypeid in (" + string.Join(",", new int[] { OrderTypes.ReturnOrder }) + @")
                           AND o.orderstatusid IN (" + string.Join(",", new int[] { 
                                                         OrderStatuses.Accepted, 
                                                         OrderStatuses.Printed, 
                                                         OrderStatuses.Shipped }) + @")
                           AND c.field4 NOT IN @testaccountids
                           AND o.orderdate BETWEEN @startdate AND @enddate 
                    GROUP  BY o.ordertypeid, day(o.orderdate), month(o.orderdate), year(o.orderdate)
                    ORDER BY year(o.orderdate), month(o.orderdate), day(o.orderdate)
                ", new
                 {
                     startdate         = filters.StartDate.BeginningOfDay(),
                     enddate           = filters.EndDate.EndOfDay(),
                     testaccountids    = Field4TestAccountIDs,

                     reporttype        = filters.ReportType,
                     orderstatusestype = filters.OrderStatusesType,
                     paymenttypes      = filters.PaymentTypeIDs,
                     revenuetype       = filters.RevenueType
                 });

                viewModel.Groups.Add(new SummaryReportRowGroup("Total Orders Shipped") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("Orders Pending") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("Returns") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
            }


            if (Request.IsAjaxRequest()) return PartialView("_ReportTable", viewModel);
            else return View("_ReportTable", viewModel);
        }

        [HttpPost]
        public ActionResult Items(SummaryReportFiltersViewModel filters)
        {
            var viewModel = new SummaryReportResultViewModel();
            viewModel.Settings = GetReportSettings(filters);

            using (var context = Exigo.Sql())
            {
                var data = context.QueryMultiple(@"
                    -- Enrollments
                    SELECT Description = od.ItemDescription, 
		                    Value = case 
                                        when @reporttype = 1 then COUNT(od.Quantity)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(od.PriceTotal)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(od.PriceTotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(od.Tax)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(od.PriceTotal)
										else -1
                                    end, 
                            [Key] = od.ItemDescription,
	                        Month = month(o.orderdate),
	                        Day = day(o.orderdate),
	                        Year = year(o.orderdate)
                    FROM   orderdetails od
		                    left join Orders o
			                    on o.orderid = od.orderid
                            LEFT JOIN customers c 
                                    ON o.customerid = c.customerid 
                            " + ((filters.PaymentTypes.Count > 0) ? "INNER JOIN Payments p ON p.OrderID = o.OrderID AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" : "") + @"
                    WHERE  c.field4 NOT IN ( '6500000009', '2928284' ) -- no test accounts 
                            AND o.orderdate BETWEEN @startdate AND @enddate 
	                        AND dateadd(month, datediff(month, 0, o.OrderDate),0) = dateadd(month, datediff(month, 0, c.CreatedDate),0)
                            AND   ((@orderstatusestype = 1 AND o.OrderStatusID in (1, 2, 3, 4, 5, 6, 7, 8, 9))
                                OR (@orderstatusestype = 2 AND o.OrderStatusID in (7, 8, 9))
                                OR (@orderstatusestype = 3 AND o.OrderStatusID in (1, 2, 3, 5, 6))
                                OR (@orderstatusestype = 4 AND o.OrderStatusID in (4)))
                    GROUP  BY od.ItemDescription, day(o.orderdate), month(o.orderdate), year(o.orderdate)
                    ORDER BY od.ItemDescription, year(o.orderdate), month(o.orderdate), day(o.orderdate)



                    -- Retail AOP
                    SELECT Description = od.ItemDescription, 
		                    Value = case 
                                        when @reporttype = 1 then COUNT(od.Quantity)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(od.PriceTotal)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(od.PriceTotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(od.Tax)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(od.PriceTotal)
										else -1
                                    end, 
                           [Key] = od.ItemDescription,
	                       Month = month(o.orderdate),
	                        Day = day(o.orderdate),
	                        Year = year(o.orderdate)
                    FROM   orderdetails od
		                    left join Orders o
			                    on o.orderid = od.orderid
                           LEFT JOIN customers c 
                                  ON o.customerid = c.customerid 
                            " + ((filters.PaymentTypes.Count > 0) ? "INNER JOIN Payments p ON p.OrderID = o.OrderID AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" : "") + @"
                    WHERE  o.ordertypeid in (4, 9)
	                       AND o.pricetypeid = 2
                           AND c.field4 NOT IN @testaccountids
                           AND o.orderdate BETWEEN @startdate AND @enddate 
                           AND    ((@orderstatusestype = 1 AND o.OrderStatusID in (1, 2, 3, 4, 5, 6, 7, 8, 9))
                                OR (@orderstatusestype = 2 AND o.OrderStatusID in (7, 8, 9))
                                OR (@orderstatusestype = 3 AND o.OrderStatusID in (1, 2, 3, 5, 6))
                                OR (@orderstatusestype = 4 AND o.OrderStatusID in (4)))
                    GROUP  BY od.ItemDescription, day(o.orderdate), month(o.orderdate), year(o.orderdate)
                    ORDER BY od.ItemDescription, year(o.orderdate), month(o.orderdate), day(o.orderdate)



                    -- IBO AOP
                    SELECT Description = od.ItemDescription, 
		                    Value = case 
                                        when @reporttype = 1 then COUNT(od.Quantity)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(od.PriceTotal)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(od.PriceTotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(od.Tax)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(od.PriceTotal)
										else -1
                                    end, 
                           [Key] = od.ItemDescription,
	                       Month = month(o.orderdate),
	                        Day = day(o.orderdate),
	                        Year = year(o.orderdate)
                    FROM   orderdetails od
		                    left join Orders o
			                    on o.orderid = od.orderid
                           LEFT JOIN customers c 
                                  ON o.customerid = c.customerid 
                            " + ((filters.PaymentTypes.Count > 0) ? "INNER JOIN Payments p ON p.OrderID = o.OrderID AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" : "") + @"
                    WHERE  o.ordertypeid in (4, 9)
	                       AND o.pricetypeid = 4
                           AND c.field4 NOT IN @testaccountids
                           AND o.orderdate BETWEEN @startdate AND @enddate 
                           AND    ((@orderstatusestype = 1 AND o.OrderStatusID in (1, 2, 3, 4, 5, 6, 7, 8, 9))
                                OR (@orderstatusestype = 2 AND o.OrderStatusID in (7, 8, 9))
                                OR (@orderstatusestype = 3 AND o.OrderStatusID in (1, 2, 3, 5, 6))
                                OR (@orderstatusestype = 4 AND o.OrderStatusID in (4)))
                    GROUP  BY od.ItemDescription, day(o.orderdate), month(o.orderdate), year(o.orderdate)
                    ORDER BY od.ItemDescription, year(o.orderdate), month(o.orderdate), day(o.orderdate)



                    -- All Other Products
                    SELECT Description = od.ItemDescription, 
		                    Value = case 
                                        when @reporttype = 1 then COUNT(od.Quantity)
                                        when @reporttype = 2 and @revenuetype = 1 then SUM(od.PriceTotal)
                                        when @reporttype = 2 and @revenuetype = 2 then SUM(od.PriceTotal)
                                        when @reporttype = 2 and @revenuetype = 3 then SUM(od.Tax)
                                        when @reporttype = 2 and @revenuetype = 4 then SUM(od.PriceTotal)
										else -1
                                    end, 
                           [Key] = od.ItemDescription,
	                       Month = month(o.orderdate),
	                        Day = day(o.orderdate),
	                        Year = year(o.orderdate)
                    FROM   orderdetails od
		                    left join Orders o
			                    on o.orderid = od.orderid
                           LEFT JOIN customers c 
                                  ON o.customerid = c.customerid 
                            " + ((filters.PaymentTypes.Count > 0) ? "INNER JOIN Payments p ON p.OrderID = o.OrderID AND p.PaymentTypeID IN (" + string.Join(",", filters.PaymentTypeIDs) + @")" : "") + @"
                    WHERE  o.ordertypeid not in (4, 9)
	                       AND o.pricetypeid not in (2, 4)
                           AND c.field4 NOT IN @testaccountids
                           AND o.orderdate BETWEEN @startdate AND @enddate 
                           AND dateadd(month, datediff(month, 0, o.OrderDate),0) <> dateadd(month, datediff(month, 0, c.CreatedDate),0)
                           AND    ((@orderstatusestype = 1 AND o.OrderStatusID in (1, 2, 3, 4, 5, 6, 7, 8, 9))
                                OR (@orderstatusestype = 2 AND o.OrderStatusID in (7, 8, 9))
                                OR (@orderstatusestype = 3 AND o.OrderStatusID in (1, 2, 3, 5, 6))
                                OR (@orderstatusestype = 4 AND o.OrderStatusID in (4)))
                    GROUP  BY od.ItemDescription, day(o.orderdate), month(o.orderdate), year(o.orderdate)
                    ORDER BY od.ItemDescription, year(o.orderdate), month(o.orderdate), day(o.orderdate)

                ", new
                 {
                     startdate         = filters.StartDate.BeginningOfDay(),
                     enddate           = filters.EndDate.EndOfDay(),
                     testaccountids    = Field4TestAccountIDs,

                     reporttype        = filters.ReportType,
                     orderstatusestype = filters.OrderStatusesType,
                     paymenttypes      = filters.PaymentTypeIDs,
                     revenuetype       = filters.RevenueType
                 });

                viewModel.Groups.Add(new SummaryReportRowGroup("Enrollments") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("Retail AOP") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("IBO AOP") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
                viewModel.Groups.Add(new SummaryReportRowGroup("All Other Products") { Rows = GetSummaryReportRows(data.Read<SummaryReportRecord>().ToList(), filters) });
            }


            if (Request.IsAjaxRequest()) return PartialView("_ReportTable", viewModel);
            else return View("_ReportTable", viewModel);
        }



        // Helpers
        private List<SummaryReportRow> GetSummaryReportRows(List<SummaryReportRecord> records, SummaryReportFiltersViewModel filters)
        {
            var rows = new List<SummaryReportRow>();

            // Prepare the view model
            var descriptions = records.Select(c => c.Description).Distinct();
            if (descriptions.Count() == 0)
            {
                descriptions = new List<string> { "" };
            }
            foreach (var description in descriptions)
            {
                var row = new SummaryReportRow
                {
                    Description = description
                };

                // Populate the dates
                var date = filters.StartDate;
                while (date <= filters.EndDate)
                {
                    var value = 0M;
                    var matchingRecords = records.Where(c =>
                        c.Description == description
                        && c.Year == date.Year
                        && c.Month == date.Month
                        && c.Day == date.Day).ToList();

                    if (matchingRecords != null && matchingRecords.Count > 0)
                    {
                        foreach (var record in matchingRecords)
                        {
                            value += record.Value;
                            row.Total += record.Value;
                        }
                    }

                    row.Columns.Add(new SummaryReportColumn
                    {
                        Date = date,
                        Value = value
                    });

                    date = date.AddDays(1);
                }

                // Add the row
                rows.Add(row);
            }

            return rows;
        }
        private SummaryReportSettings GetReportSettings(SummaryReportFiltersViewModel filters)
        {
            var settings = new SummaryReportSettings();
            settings.ValueFormat =
                (filters.ReportType == SummaryReportType.TransactionCounts)
                ? SummaryReportFormatType.Int
                : SummaryReportFormatType.Currency;

            return settings;
        }
    }
}