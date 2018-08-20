using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminDashboard.Models.FusionCharts
{
    public class CategorySetDetail : FusionChartCategorySetDetail
    {
        public CategorySetDetail()
        {
        }
        public CategorySetDetail(string label)
        {
            base.Label = label;
        }
    }  
}