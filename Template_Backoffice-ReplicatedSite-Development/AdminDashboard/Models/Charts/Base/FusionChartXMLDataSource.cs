using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminDashboard.Models.FusionCharts
{
    public abstract class FusionChartXMLDataSource
    {
        public string Caption                           = string.Empty;
        public string xAxisName                         = string.Empty;
        public string yAxisName                         = string.Empty;

        public string CanvasBackgroundColor             = "FFFFFF";
        public bool ShowChartBorder                     = false;
        public bool ShowValues                          = true;
        public bool ShowLegend                          = true;
        public string LegendPosition                    = "bottom";
        public string LabelDisplayMode                  = "auto";

        public int DecimalPlaces                        = 0;
        public bool FormatNumbers                       = true;
        public bool FormatNumbersToScale                = false;
        public string NumberPrefix                      = "";
    }
}