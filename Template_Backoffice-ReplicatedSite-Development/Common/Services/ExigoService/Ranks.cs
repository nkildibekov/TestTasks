using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace ExigoService
{
    public static partial class Exigo
    {
        public static IEnumerable<Rank> GetRanks()
        {
            var ranks = new List<Rank>();
            using (var context = Exigo.Sql())
            {
                ranks = context.Query<Rank>(@"
                        SELECT 
	                        r.RankID
	                        ,r.RankDescription

                        FROM
	                        Ranks r                                
                        ").OrderBy(c => c.RankID).ToList();
            }

            //Ensure that rank 0 exists
            if(ranks.Where(c => c.RankID == 0).FirstOrDefault() == null)
            {
                ranks.Insert(0, new Rank() { RankID = 0, RankDescription = "" });
            }

            foreach (var rank in ranks)
            {
                yield return rank;
            }
        }

        public static Rank GetRank(int rankID)
        {
            return GetRanks()
                .Where(c => c.RankID == rankID)
                .FirstOrDefault();
        }

        public static IEnumerable<Rank> GetNextRanks(int rankID)
        {
            return GetRanks()
                .Where(c => c.RankID > rankID)
                .OrderBy(c => c.RankID)
                .ToList();
        }
        public static Rank GetNextRank(int rankID)
        {
            return GetNextRanks(rankID).FirstOrDefault();
        }

        public static IEnumerable<Rank> GetPreviousRanks(int rankID)
        {
            return GetRanks()
                .Where(c => c.RankID < rankID)
                .OrderByDescending(c => c.RankID)
                .ToList();
        }
        public static Rank GetPreviousRank(int rankID)
        {
            return GetPreviousRanks(rankID).FirstOrDefault();
        }

        public static CustomerRankCollection GetCustomerRanks(GetCustomerRanksRequest request)
        {            
            var result = new CustomerRankCollection();            
            var periodID = (request.PeriodID != null) ? request.PeriodID : Exigo.GetCurrentPeriod(request.PeriodTypeID).PeriodID;            

            //Get the highest paid rank in any period from the customer record
            var highestRankAchieved = new Rank();

            using (var context = Exigo.Sql())
            {
                highestRankAchieved = context.Query<Rank>(@"
                        SELECT 
	                        c.RankID
	                        ,r.RankDescription	

                        FROM
	                        Customers c
	                        INNER JOIN Ranks r
		                        ON r.RankID = c.RankID

                        WHERE
	                        c.CustomerID = @customerid                       
                        ", new
                        {
                            customerid = request.CustomerID
                        }).FirstOrDefault();

                if (highestRankAchieved != null)
                {
                    result.HighestPaidRankInAnyPeriod = highestRankAchieved;
                }
            }

            //Get the current period rank for the period/period type specified
            var currentPeriodRank = new Rank();

            using (var context = Exigo.Sql())
            {
                currentPeriodRank = context.Query<Rank>(@"
                        SELECT 
	                        RankID = pv.PaidRankID
	                        ,r.RankDescription	

                        FROM
	                        PeriodVolumes pv
	                        INNER JOIN Ranks r
		                        ON r.RankID = pv.PaidRankID	

                        WHERE
	                        pv.CustomerID = @customerid
	                        AND pv.PeriodTypeID = @periodtypeid
	                        AND pv.PeriodID = @periodid                      
                        ", new
                         {
                             customerid = request.CustomerID,
                             periodtypeid = request.PeriodTypeID,
                             periodid = periodID
                         }).FirstOrDefault();

                if (currentPeriodRank != null)
                {
                    result.CurrentPeriodRank = currentPeriodRank;
                }
            }

            //Get the highest paid rank up to the specified period
            var highestPaidRankUpToPeriod = new Rank();

            using (var context = Exigo.Sql())
            {
                highestPaidRankUpToPeriod = context.Query<Rank>(@"
                        SELECT 
	                        pv.RankID
	                        ,r.RankDescription	

                        FROM
	                        PeriodVolumes pv
	                        INNER JOIN Ranks r
		                        ON r.RankID = pv.RankID	

                        WHERE
	                        pv.CustomerID = @customerid
	                        AND pv.PeriodTypeID = @periodtypeid
	                        AND pv.PeriodID = @periodid                      
                        ", new
                         {
                             customerid = request.CustomerID,
                             periodtypeid = request.PeriodTypeID,
                             periodid = periodID
                         }).FirstOrDefault();

                if (highestPaidRankUpToPeriod != null)
                {
                    result.HighestPaidRankUpToPeriod = highestPaidRankUpToPeriod;
                }
            }

            return result;
        }
    }
}