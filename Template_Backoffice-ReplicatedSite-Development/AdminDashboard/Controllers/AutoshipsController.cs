using Common.Helpers;
using AdminDashboard.Models;
using AdminDashboard.Services;
using AdminDashboard.ViewModels;
using AdminDashboard.Models.FusionCharts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AdminDashboard.Controllers
{
    public class AutoshipsController : Controller
    {
        public ActionResult Index()
        {
            var model = new ProjectedAutoshipsViewModel();


            var helper = new SqlHelper();
            var dataset = helper.GetSet(string.Format(@"
                DECLARE @pa_StartDate AS DATETIME = convert(DATETIME, '{0}');
                DECLARE @pa_EndDate AS DATETIME = convert(DATETIME, '{1}');

                SELECT ""Month"" = month(aos.ScheduledDate)
	                 , ""Day"" = day(aos.ScheduledDate)
	                 , ""Year"" = year(aos.ScheduledDate)
	                 , Total = sum(ao.SubTotal)
                FROM
	                AutoOrders ao
	                INNER JOIN AutoOrderSchedules aos
		                ON aos.AutoOrderID = ao.AutoOrderID

                WHERE
	                ao.AutoOrderStatusID = 0
	                AND aos.ScheduledDate >= @pa_StartDate
	                AND aos.ScheduledDate < @pa_EndDate

                GROUP BY
	                month(aos.ScheduledDate)
                  , day(aos.ScheduledDate)
                  , year(aos.ScheduledDate)

                ORDER BY
	                year(aos.ScheduledDate) ASC
                  , month(aos.ScheduledDate) ASC
                  , day(aos.ScheduledDate) ASC

            ", new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).ToShortDateString(),
                new DateTime(DateTime.Now.AddMonths(1).Year, DateTime.Now.AddMonths(1).Month, DateTime.Now.AddMonths(1).Day).ToShortDateString()));




            // Divide our dataset into separate tables for easy reading
            var projectedAutoshipsData = dataset.Tables[0];


            // Populate our details collection
            var details = new List<ProjectedAutoshipDetail>();
            foreach (DataRow row in projectedAutoshipsData.Rows)
            {
                var detail   = new ProjectedAutoshipDetail();
                detail.Month = Convert.ToInt32(row["Month"]);
                detail.Day   = Convert.ToInt32(row["Day"]);
                detail.Year  = Convert.ToInt32(row["Year"]);
                detail.Total = Convert.ToDecimal(row["Total"]);
                details.Add(detail);
            }
            model.Details = details;

            // Start filling out our view model
            var highesttotaldetail = model.Details.OrderByDescending(c => c.Total).FirstOrDefault();
            var lowesttotaldetail  = model.Details.OrderBy(c => c.Total).FirstOrDefault();

            if (highesttotaldetail != null)
            {
                model.HighestTotal = highesttotaldetail.Total;
                model.HighestTotalDate = highesttotaldetail.Date;
            }
            if (lowesttotaldetail != null)
            {
                model.LowestTotal = lowesttotaldetail.Total;
                model.LowestTotalDate = lowesttotaldetail.Date;
            }

            if (model.Details.Count() > 0)
            {
                model.AverageTotal = model.Details.Average(c => c.Total);
            }


            var detailsByWeekDay = (from d in model.Details
                                    group d by d.WeekDay into g
                                    select new
                                    {
                                        WeekDay = g.Key,
                                        Total = g.Sum(a => a.Total)
                                    });
            if (detailsByWeekDay.Count() > 0)
            {
                model.HighestTotalDayOfWeek = detailsByWeekDay.OrderByDescending(c => c.Total).First().WeekDay;
                model.LowestTotalDayOfWeek = detailsByWeekDay.OrderBy(c => c.Total).First().WeekDay;
            }





            // Charts
            var dataSource = new SingleSeriesChartXMLDataSource();


            // Customize our chart
            dataSource.yAxisName = "Projected Revenue";
            dataSource.ShowValues = false;


            // Add our sets
            foreach (DataRow row in projectedAutoshipsData.Rows)
            {
                var month = Convert.ToInt32(row["Month"]);
                var day   = Convert.ToInt32(row["Day"]);
                var year  = Convert.ToInt32(row["Year"]);
                var value = Convert.ToDecimal(row["Total"]);
                var label = string.Format("{0} {1}/{2}", new DateTime(year, month, day).ToString("dddd").Substring(0, 3), month, day);


                var set = new ChartDataSetDetail();
                set.Label = label;
                set.Value = value;
                set.ToolHelp = label;
                dataSource.Sets.Add(set);
            }


            // Add some trend lines to the highest and lowest sets
            var highestValueSet = dataSource.Sets.OrderByDescending(c => c.Value).FirstOrDefault();
            if (highestValueSet != null)
            {
                highestValueSet.VerticalTrendLine = new VerticalTrendLine()
                {
                    Label                  = "▲",
                    LineColor              = "00CC00",
                    LabelVerticalAlignment = "top",
                    LinePosition           = 0M,
                    LabelPosition          = 0.999M,
                    LineThickness          = 2,
                    ShowLabelBorder        = false
                };
            }

            var lowestValueSet = dataSource.Sets.OrderBy(c => c.Value).FirstOrDefault();
            if (lowestValueSet != null)
            {
                lowestValueSet.VerticalTrendLine = new VerticalTrendLine()
                {
                    Label                  = "▼",
                    LineColor              = "FF0000",
                    LabelVerticalAlignment = "bottom",
                    LinePosition           = 0M,
                    LabelPosition          = 0.005M,
                    LineThickness          = 2,
                    ShowLabelBorder        = false
                };
            }


            // Add our average trend line
            if (dataSource.Sets.Count > 0)
            {
                var meanValue = dataSource.Sets.Average(c => Convert.ToDecimal(c.Value));
                dataSource.HorizontalTrendLines.Add(new HorizontalTrendLine()
                {
                    StartValue   = meanValue,
                    DisplayValue = "Average",
                    ToolText     = string.Format("The average daily autoship revenue ({0:C})", meanValue),
                    LineColor    = "006699"
                });
            }


            // Return the formatted XML
            model.ProjectedAutoshipsChartXml = dataSource.ToString();


            if (Request.IsAjaxRequest()) return PartialView(model);
            else return View(model);
        }
    }
}
