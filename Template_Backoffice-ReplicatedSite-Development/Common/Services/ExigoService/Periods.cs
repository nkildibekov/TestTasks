using Common;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dapper;
using System;

namespace ExigoService
{
    public static partial class Exigo
    {
        public static IEnumerable<Period> GetPeriods(GetPeriodsRequest request)
        {
            var periods = new List<Period>();
            using (var context = Exigo.Sql())
            {
                periods = context.Query<Period>(@"
                                SELECT p.PeriodTypeID
                                      , p.PeriodID
                                      , p.PeriodDescription
                                      , p.StartDate
                                      , p.EndDate
                                      , p.AcceptedDate
                                FROM Periods p
                                WHERE p.PeriodTypeID = @PeriodTypeID
                                    AND p.PeriodID IN @PeriodID
                    ", new
                     {
                         PeriodTypeID = request.PeriodTypeID,
                         PeriodID = request.PeriodIDs.ToList()
                     }).ToList();
            }


            // Optionally filter by the customer.
            // If the customer is provided, only periods the customer was a part of will be returned.
            if (request.CustomerID != null)
            {
                var customer = new Customer();
                using (var context = Exigo.Sql())
                {
                    customer = context.Query<Customer>(@"
                                SELECT CreatedDate
                                FROM Customers
                                WHERE CustomerID = @CustomerID
                    ", new
                     {
                         CustomerID = request.CustomerID
                     }).FirstOrDefault();
                }

                if (customer != null)
                {
                    periods = periods.Where(c => c.EndDate >= customer.CreatedDate).ToList();
                }
            }

            if (periods == null) return null;

            return periods;
        }
        public static Period GetCurrentPeriod(int periodTypeID)
        {
            var cachekey = GlobalSettings.Exigo.Api.CompanyKey + "CurrentPeriod_" + periodTypeID.ToString();
            var p = (Period)HttpRuntime.Cache[cachekey];
            if (HttpRuntime.Cache[cachekey] == null)
            {
                var period = new Period();
                using (var context = Exigo.Sql())
                {
                    period = context.Query<Period>(@"
                            SELECT p.PeriodTypeID
                                , p.PeriodID
                                , p.PeriodDescription
                                , p.StartDate
                                , p.EndDate
                                , p.AcceptedDate
                            FROM Periods p
                            WHERE p.PeriodTypeID = @PeriodTypeID
                                AND @CurrentDate between p.StartDate and dateadd(day, 1, p.EndDate)
                            ORDER BY p.AcceptedDate desc, p.EndDate desc
                            ", new
                             {
                                 PeriodTypeID = periodTypeID,
                                 CurrentDate = DateTime.Now.ToCST()
                             }).FirstOrDefault();
                }

                HttpRuntime.Cache[cachekey] = (Period)period;
            }

            return (Period)HttpRuntime.Cache[cachekey];
        }
    }
}
