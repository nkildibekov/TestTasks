using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminDashboard.Models.FusionCharts
{
    public abstract class FusionChartDataSetDetail
    {
        public string ToolHelp { get; set; }
        public string Label { get; set; }
        public object Value { get; set; }

        public VerticalTrendLine VerticalTrendLine = null;
    }
}