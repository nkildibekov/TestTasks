using Common.Helpers;
using AdminDashboard.Models;
using AdminDashboard.Services;
using AdminDashboard.ViewModels;
using AdminDashboard.Models.FusionCharts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace AdminDashboard.Controllers
{
    public class CommissionsController : Controller
    {
        public ActionResult Index()
        {
            // Get the data
            var helper = new SqlHelper();
            var dataset = helper.GetSet(string.Format(@"
                DECLARE @c_StartDate AS DATETIME = convert(DATETIME, '{0}');
                DECLARE @c_EndDate AS DATETIME = convert(DATETIME, '{1}');

                -- Earnings
                SELECT c.CommissionRunID
	                    , cr.CommissionRunDescription
	                    , cr.AcceptedDate
	                    , cr.PeriodTypeID
	                    , pr.PaidRankID
	                    , PaidRankDescription = r.RankDescription
	                    , Total = sum(c.Total)
	                    , Earnings = sum(c.Earnings)
	                    , PaidAsRankCount = count(pr.PaidRankID)
                FROM
	                Commissions c
	                INNER JOIN CommissionRuns cr
		                ON cr.CommissionRunID = c.CommissionRunID
	                INNER JOIN PeriodVolumes pr
		                ON pr.CustomerID = c.CustomerID
		                AND pr.PeriodID = cr.PeriodID
		                AND pr.PeriodTypeID = cr.PeriodTypeID
	                INNER JOIN Ranks r
		                ON pr.PaidRankID = r.RankID
                WHERE
	                cr.CommissionRunStatusID IN (3)
	                AND cr.AcceptedDate >= @c_StartDate
	                AND cr.AcceptedDate < @c_EndDate

                GROUP BY
	                c.CommissionRunID
                    , cr.CommissionRunDescription
                    , cr.AcceptedDate
                    , cr.PeriodTypeID
                    , pr.PaidRankID
                    , r.RankDescription

                ORDER BY
	                c.CommissionRunID ASC
                    , cr.PeriodTypeID ASC
                    , pr.PaidRankID ASC



                -- Bonuses
                SELECT cb.CommissionRunID
	                    , cr.CommissionRunDescription
	                    , cr.AcceptedDate
	                    , cr.PeriodTypeID
	                    , cb.BonusID
	                    , b.BonusDescription
	                    , BonusAmount = sum(cb.Amount)
                FROM
	                CommissionBonuses cb
	                INNER JOIN CommissionRuns cr
		                ON cr.CommissionRunID = cb.CommissionRunID
	                INNER JOIN Bonuses b
		                ON b.BonusID = cb.BonusID AND b.PeriodTypeID = cr.PeriodTypeID

                WHERE
	                cr.CommissionRunStatusID IN (3)
	                AND cr.AcceptedDate >= @c_StartDate
	                AND cr.AcceptedDate < @c_EndDate

                GROUP BY
	                cb.CommissionRunID
                    , cr.CommissionRunDescription
                    , cr.AcceptedDate
                    , cr.PeriodTypeID
                    , cb.BonusID
                    , b.BonusDescription

                ORDER BY
	                cb.CommissionRunID ASC
                    , cr.PeriodTypeID ASC
                    , cb.BonusID ASC

            ", new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, DateTime.Now.AddYears(-1).Day).ToShortDateString(),
                new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).ToShortDateString()));



            var model = new CommissionSummaryViewModel(dataset);



            if (Request.IsAjaxRequest()) return PartialView(model);
            else return View(model);
        }
    }
}
