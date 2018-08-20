using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public class SummaryReportRow
    {
        public SummaryReportRow()
        {
            this.Columns = new List<SummaryReportColumn>();            
        }

        public string Description { get; set; }
        public List<SummaryReportColumn> Columns { get; set; }
        public decimal Total { get; set; }
    }
}