using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public class SummaryReportResultViewModel
    {
        public SummaryReportResultViewModel()
        {
            this.Groups = new List<SummaryReportRowGroup>();
            
        }

        public List<SummaryReportRowGroup> Groups { get; set; }
        public SummaryReportSettings Settings { get; set; }
    }
}