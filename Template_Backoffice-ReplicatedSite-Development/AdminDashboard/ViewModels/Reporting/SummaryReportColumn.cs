using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public class SummaryReportColumn
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }
}