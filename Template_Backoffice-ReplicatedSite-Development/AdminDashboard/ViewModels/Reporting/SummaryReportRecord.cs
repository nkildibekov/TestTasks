using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public class SummaryReportRecord
    {
        public object Key { get; set; }
        public string Description { get; set; }
        public decimal Value { get; set; }

        public int Month { get; set; }
        public int Day { get; set; }
        public int Year { get; set; }

        public DateTime Date
        {
            get { return new DateTime(Year, Month, Day); }
        }
    }
}