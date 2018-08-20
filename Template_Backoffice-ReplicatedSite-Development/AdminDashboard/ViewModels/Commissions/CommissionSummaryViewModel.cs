using Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public class CommissionSummaryViewModel
    {
        public CommissionSummaryViewModel(DataSet dataset)
        {
            // Divide our dataset into separate tables for easy reading
            var commissionEarningsData = dataset.Tables[0];
            var commissionBonusesData = dataset.Tables[1];


            // Populate our details collections
            CommissionEarningsDetails = new List<CommissionEarningsDetail>();
            foreach (DataRow row in commissionEarningsData.Rows)
            {
                var detail                      = new CommissionEarningsDetail();
                detail.CommissionRunID          = Convert.ToInt32(row["CommissionRunID"]);
                detail.CommissionRunDescription = row["CommissionRunDescription"].ToString();
                detail.AcceptedDate             = Convert.ToDateTime(row["AcceptedDate"]);
                detail.PeriodTypeID             = Convert.ToInt32(row["PeriodTypeID"]);
                detail.PaidRankID               = Convert.ToInt32(row["PaidRankID"]);
                detail.PaidRankDescription      = row["PaidRankDescription"].ToString();
                detail.Earnings                 = Convert.ToDecimal(row["Earnings"]);
                detail.PaidAsRankCount          = Convert.ToInt32(row["PaidAsRankCount"]);
                CommissionEarningsDetails.Add(detail);
            }

            CommissionBonusesDetails = new List<CommissionBonusDetail>();
            foreach (DataRow row in commissionBonusesData.Rows)
            {
                var detail                      = new CommissionBonusDetail();
                detail.CommissionRunID          = Convert.ToInt32(row["CommissionRunID"]);
                detail.CommissionRunDescription = row["CommissionRunDescription"].ToString();
                detail.AcceptedDate             = Convert.ToDateTime(row["AcceptedDate"]);
                detail.PeriodTypeID             = Convert.ToInt32(row["PeriodTypeID"]);
                detail.BonusID                  = Convert.ToInt32(row["BonusID"]);
                detail.BonusDescription         = row["BonusDescription"].ToString();
                detail.BonusAmount              = Convert.ToDecimal(row["BonusAmount"]);
                CommissionBonusesDetails.Add(detail);
            }

            // Get the weekly totals
            var weeklyDetailsByRunID =
                CommissionEarningsDetails
                    .Where(c => c.PeriodTypeID == (int)PeriodTypes.Weekly)
                    .GroupBy(c => new { c.CommissionRunID, c.AcceptedDate }, (key, group) => new
                    {
                        CommissionRunID = key.CommissionRunID,
                        AcceptedDate    = key.AcceptedDate,
                        Payout          = group.Sum(a => a.Earnings)
                    });

            var orderedWeeklyPayouts = weeklyDetailsByRunID.OrderByDescending(c => c.Payout);
            if (orderedWeeklyPayouts.Count() > 0)
            {
                HighestWeeklyPayout     = orderedWeeklyPayouts.First().Payout;
                HighestWeeklyPayoutDate = orderedWeeklyPayouts.First().AcceptedDate;
                LowestWeeklyPayout      = orderedWeeklyPayouts.Last().Payout;
                LowestWeeklyPayoutDate  = orderedWeeklyPayouts.Last().AcceptedDate;
                AverageWeeklyPayout     = orderedWeeklyPayouts.Average(c => c.Payout);
            }


            // Get the monthly totals
            var monthlyDetailsByRunID =
                CommissionEarningsDetails
                        .Where(c => c.PeriodTypeID == (int)PeriodTypes.Weekly)
                        .GroupBy(c => new { c.CommissionRunID, c.AcceptedDate }, (key, group) => new
                        {
                            CommissionRunID = key.CommissionRunID,
                            AcceptedDate    = key.AcceptedDate,
                            Payout          = group.Sum(a => a.Earnings)
                        });


            var orderedMonthlyPayouts = monthlyDetailsByRunID.OrderByDescending(c => c.Payout);
            if (orderedMonthlyPayouts.Count() > 0)
            {
                HighestMonthlyPayout     = orderedMonthlyPayouts.First().Payout;
                HighestMonthlyPayoutDate = orderedMonthlyPayouts.First().AcceptedDate;
                LowestMonthlyPayout      = orderedMonthlyPayouts.Last().Payout;
                LowestMonthlyPayoutDate  = orderedMonthlyPayouts.Last().AcceptedDate;
                AverageMonthlyPayout     = orderedMonthlyPayouts.Average(c => c.Payout);
            }



            // Charts
            PopulateCommissionBonusesChartXml();
        }

        public List<CommissionEarningsDetail> CommissionEarningsDetails { get; set; }
        public List<CommissionBonusDetail> CommissionBonusesDetails { get; set; }


        public decimal HighestWeeklyPayout { get; set; }
        public DateTime HighestWeeklyPayoutDate { get; set; }
        public decimal LowestWeeklyPayout { get; set; }
        public DateTime LowestWeeklyPayoutDate { get; set; }
        public decimal AverageWeeklyPayout { get; set; }
        public decimal HighestMonthlyPayout { get; set; }
        public DateTime HighestMonthlyPayoutDate { get; set; }
        public decimal LowestMonthlyPayout { get; set; }
        public DateTime LowestMonthlyPayoutDate { get; set; }
        public decimal AverageMonthlyPayout { get; set; }

        public string CommissionBonusesChartXml { get; set; }


        public void PopulateCommissionBonusesChartXml()
        {
            // Establish our chart variables
            var xAxisLabels = new List<string>();
            var seriesCollection = new List<ChartSeries>();


            // Group our data to ensure that we show accurate data.
            var groupedData = CommissionBonusesDetails
                .GroupBy(c => new { c.BonusDescription, c.AcceptedDate }, (key, group) => new
                {
                    key.BonusDescription,
                    key.AcceptedDate,
                    BonusAmount = group.Sum(a => a.BonusAmount)
                });


            // Loop through each row and define our variables from the data
            foreach (var detail in groupedData)
            {
                // X-axis
                var monthDescription = detail.AcceptedDate.ToString("MMMM yyyy");
                if (!xAxisLabels.Contains(monthDescription)) xAxisLabels.Add(monthDescription);

                // ConnectionStrings series
                var seriesName = detail.BonusDescription;
                var existingSeries = seriesCollection.Where(c => c.SeriesName == seriesName).Select(c => c).FirstOrDefault();
                if (existingSeries == null)
                {
                    var newSeries = new ChartSeries();
                    newSeries.SeriesName = seriesName;
                    newSeries.Values.Add(detail.BonusAmount);
                    seriesCollection.Add(newSeries);
                }
                else
                {
                    existingSeries.Values.Add(detail.BonusAmount);
                }
            }


            // Compile the individual sections. Let's start with the categories.
            var categoriesXml = string.Empty;
            foreach (var label in xAxisLabels)
            {
                categoriesXml += string.Format(@"<category label='{0}' />", label);
            }
            categoriesXml = "<categories>" + categoriesXml + "</categories>";


            // Next, compile the datasets
            var datasetXml = string.Empty;
            foreach (var series in seriesCollection)
            {
                // Dataset Values
                var datasetValuesJson = string.Empty;
                foreach (var value in series.Values)
                {
                    datasetValuesJson += string.Format("<set value='{0}' />", value);
                }
                datasetXml += string.Format("<dataset seriesName='{0}'>", series.SeriesName) + datasetValuesJson + "</dataset>";
            }


            // Compile the Json
            var json = new StringBuilder();
            json.AppendFormat("<chart caption='' xAxisName='' yAxisName='Payouts' bgcolor='FFFFFF, FFFFFF' showBorder='0' showValues='0' labelDisplay='auto' showvalues='0' showSum='1' decimals='2' formatNumberScale='0' numberprefix='$'>{0}{1}</chart>",
                categoriesXml,
                datasetXml);


            // Set the Json to the corresponding property
            CommissionBonusesChartXml = json.ToString();
        }
    }

    public class CommissionEarningsDetail
    {
        public int CommissionRunID { get; set; }
        public string CommissionRunDescription { get; set; }
        public DateTime AcceptedDate { get; set; }
        public int PeriodTypeID { get; set; }
        public int PaidRankID { get; set; }
        public int PaidAsRankCount { get; set; }
        public string PaidRankDescription { get; set; }
        public decimal Earnings { get; set; }
    }

    public class CommissionBonusDetail
    {
        public int CommissionRunID { get; set; }
        public string CommissionRunDescription { get; set; }
        public DateTime AcceptedDate { get; set; }
        public int PeriodTypeID { get; set; }
        public int BonusID { get; set; }
        public string BonusDescription { get; set; }
        public decimal BonusAmount { get; set; }
    }
}