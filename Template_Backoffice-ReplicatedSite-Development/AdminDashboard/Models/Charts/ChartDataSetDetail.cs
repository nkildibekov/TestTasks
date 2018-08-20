using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminDashboard.Models.FusionCharts
{
    public class ChartDataSetDetail : FusionChartDataSetDetail
    {
        public ChartDataSetDetail()
        {
        }
        public ChartDataSetDetail(object value)
        {
            base.Value = value;
        }
    }
}