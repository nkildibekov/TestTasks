using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace AdminDashboard.Models.FusionCharts
{
    public class HorizontalTrendLine
    {
        public HorizontalTrendLine()
        {

        }

        public decimal StartValue                       = 0M;                       // The starting value for the trendline.
        public decimal? EndValue                        = null;                       // The ending y-axis value for the trendline. 
        public string DisplayValue                      = string.Empty;             // If you want to display a string caption for the trend line by its side, you can use this attribute. When you don't supply this attribute, it automatically takes the value of startValue.
        public string ToolText                          = string.Empty;             // Custom tool-text for this trendline/zone.
        public string LineColor                         = "999999";                 // Color of the trend line and its associated text.
        public bool ShowValueOnRight                    = true;                     // Whether to show the trend line value on left side or right side of chart. This is particularly useful when the trend line display values on the chart are colliding with divisional lines values on the chart.
        public bool IsTrendZone                         = false;                    // Whether the trend will display a line, or a zone (filled colored rectangle).
        public bool ShowOnTop                           = true;                     // Whether the trend line/zone will be displayed over data plots or under them.
        public int LineThickness                        = 1;                        // Thickness in pixels of the vertical separator line
        public decimal LineOpacity                      = 100M;                     // (0-100) Alpha of the vertical separator line
        public bool DashedLine                          = false;                    // Is the line dashed?
        public int DashLength                           = 1;                        // The length of each dash in the line.
        public int DashGap                              = 1;                        // The space in between each gap of the dashed line.

        public override string ToString()
        {
            StringBuilder xml = new StringBuilder();


            xml.AppendFormat("<line ");
            xml.AppendFormat("startValue='{0}' ",       StartValue);
            if(EndValue != null) xml.AppendFormat("endValue='{0}' ",         EndValue);
            xml.AppendFormat("isTrendZone='{0}' ",      Convert.ToInt32(IsTrendZone));
            xml.AppendFormat("showOnTop='{0}' ",        Convert.ToInt32(ShowOnTop));
            xml.AppendFormat("valueOnRight='{0}' ",     Convert.ToInt32(ShowValueOnRight));
            xml.AppendFormat("displayValue='{0}' ",     DisplayValue);
            xml.AppendFormat("toolText='{0}' ",         ToolText);
            xml.AppendFormat("color='{0}' ",            LineColor);
            xml.AppendFormat("thickness='{0}' ",        LineThickness);
            xml.AppendFormat("alpha='{0}' ",            LineOpacity);
            xml.AppendFormat("dashed='{0}' ",           Convert.ToInt32(DashedLine));
            xml.AppendFormat("dashLen='{0}' ",          DashLength);
            xml.AppendFormat("dashGap='{0}' ",          DashGap);
            xml.AppendFormat(" />");


            return xml.ToString();
        }
    }
}