using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace AdminDashboard.Models.FusionCharts
{
    public class PieChartXMLDataSource : FusionChartXMLDataSource
    {
        public PieChartXMLDataSource()
        {
            Sets = new List<ChartDataSetDetail>();
        }

        #region Properties
        public List<ChartDataSetDetail> Sets { get; set; }

        public bool EnableSmartLabels                   = true;
        public bool EnableRotation                      = true;
        public bool ShowPercentValues                   = false;
        public bool ShowLabels                          = false;
        public bool Use3DLighting                       = true;
        #endregion

        #region Overrides
        public override string ToString()
        {
            StringBuilder xml = new StringBuilder();

            // Start the chart tag
            xml.AppendFormat("<chart ");
            xml.AppendFormat("xAxisName='{0}' ",            xAxisName);
            xml.AppendFormat("yAxisName='{0}' ",            yAxisName);
            xml.AppendFormat("enableSmartLabels='{0}' ",    Convert.ToInt32(EnableSmartLabels));
            xml.AppendFormat("enableRotation='{0}' ",       Convert.ToInt32(EnableRotation));
            xml.AppendFormat("bgcolor='{0}' ",              CanvasBackgroundColor);
            xml.AppendFormat("showBorder='{0}' ",           Convert.ToInt32(ShowChartBorder));
            xml.AppendFormat("labelDisplay='{0}' ",         LabelDisplayMode);
            xml.AppendFormat("showPercentValues='{0}' ",    Convert.ToInt32(ShowPercentValues));
            xml.AppendFormat("showValues='{0}' ",           Convert.ToInt32(ShowValues));
            xml.AppendFormat("showLabels='{0}' ",           Convert.ToInt32(ShowLabels));
            xml.AppendFormat("showLegend='{0}' ",           Convert.ToInt32(ShowLegend));
            xml.AppendFormat("legendPosition='{0}' ",       LegendPosition);
            xml.AppendFormat("decimals='{0}' ",             DecimalPlaces);
            xml.AppendFormat("formatNumberScale='{0}' ",    Convert.ToInt32(FormatNumbersToScale));
            xml.AppendFormat("formatNumber='{0}' ",         Convert.ToInt32(FormatNumbers));
            xml.AppendFormat("numberPrefix='{0}' ",         NumberPrefix);
            xml.AppendFormat("use3DLighting='{0}' ",        Convert.ToInt32(Use3DLighting));
            xml.AppendFormat(">");

       
            // Add the sets
            foreach(var set in Sets)
            {
                xml.AppendFormat("<set ");
                xml.AppendFormat("label='{0}' ",              set.Label);
                xml.AppendFormat("value='{0}' ",              set.Value);
                xml.AppendFormat("toolHelp='{0}' ",           set.ToolHelp);
                xml.AppendFormat(" />");
            }


            // End the chart tag
            xml.AppendFormat("</chart>");

            return xml.ToString();
        }
        #endregion

        #region Methods
        public void AddSeries(string label, decimal value)
        {
            var set = this.Sets.Where(c => c.Label == label).FirstOrDefault();
            if(set == null) 
            {
                set = new ChartDataSetDetail() 
                { 
                    Label = label,
                    Value = value,
                    ToolHelp = label
                };
                this.Sets.Add(set);
            }
        }
        #endregion
    }
}