using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public class SummaryReportRowGroup
    {
        public SummaryReportRowGroup()
        {
            this.Rows = new List<SummaryReportRow>();            
        }
        public SummaryReportRowGroup(string title)
        {
            this.Title = title;
            this.Rows = new List<SummaryReportRow>();
        }

        public string Title { get; set; }
        public List<SummaryReportRow> Rows { get; set; }
    }
}