using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public class ProjectedAutoshipsViewModel
    {
        public IEnumerable<ProjectedAutoshipDetail> Details { get; set; }

        public decimal HighestTotal { get; set; }
        public DateTime HighestTotalDate { get; set; }
        public DayOfWeek HighestTotalDayOfWeek { get; set; }
        public decimal AverageTotal { get; set; }
        public decimal LowestTotal { get; set; }
        public DateTime LowestTotalDate { get; set; }
        public DayOfWeek LowestTotalDayOfWeek { get; set; }
        public string ProjectedAutoshipsChartXml { get; set; }
    }

    public class ProjectedAutoshipDetail
    {
        public int Month { get; set; }
        public int Day { get; set; }
        public int Year { get; set; }
        public DateTime Date
        {
            get
            {
                return new DateTime(this.Year, this.Month, this.Day);
            }
            set
            {
                this.Month = value.Month;
                this.Day = value.Day;
                this.Year = value.Year;
            }
        }
        public DayOfWeek WeekDay
        {
            get
            {
                return this.Date.DayOfWeek;
            }
        }

        public decimal Total { get; set; }
    }
}