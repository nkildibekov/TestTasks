using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace AdminDashboard.Models.FusionCharts
{
    public class MultiSeriesChartXMLDataSource : FusionChartXMLDataSource
    {
        public MultiSeriesChartXMLDataSource()
        {
            Categories = new List<CategorySetDetail>();
            DataSets = new List<ChartDataSet>();
            HorizontalTrendLines = new List<HorizontalTrendLine>();
        }

        #region Properties
        public List<CategorySetDetail> Categories { get; set; }
        public List<ChartDataSet> DataSets { get; set; }
        public List<HorizontalTrendLine> HorizontalTrendLines { get; set; }

        public bool ShowSum             = false;
        #endregion

        #region Overrides
        public override string ToString()
        {
            StringBuilder xml = new StringBuilder();

            // Start the chart tag
            xml.AppendFormat("<chart ");
            xml.AppendFormat("xAxisName='{0}' ",            xAxisName);
            xml.AppendFormat("yAxisName='{0}' ",            yAxisName);
            xml.AppendFormat("bgcolor='{0}' ",              CanvasBackgroundColor);
            xml.AppendFormat("showBorder='{0}' ",           Convert.ToInt32(ShowChartBorder));
            xml.AppendFormat("labelDisplay='{0}' ",         LabelDisplayMode);
            xml.AppendFormat("showValues='{0}' ",           Convert.ToInt32(ShowValues));
            xml.AppendFormat("showSum='{0}' ",              Convert.ToInt32(ShowSum));
            xml.AppendFormat("showLegend='{0}' ",           Convert.ToInt32(ShowLegend));
            xml.AppendFormat("legendPosition='{0}' ",       LegendPosition);
            xml.AppendFormat("decimals='{0}' ",             DecimalPlaces);
            xml.AppendFormat("formatNumber='{0}' ",         Convert.ToInt32(FormatNumbers));
            xml.AppendFormat("formatNumberScale='{0}' ",    Convert.ToInt32(FormatNumbersToScale));
            xml.AppendFormat("numberPrefix='{0}' ",         NumberPrefix);
            xml.AppendFormat(">");


            // Add the categories
            xml.AppendFormat("<categories>");
            foreach(var category in Categories)
            {
                xml.AppendFormat("<category ");
                xml.AppendFormat("label='{0}' ",            category.Label);
                xml.AppendFormat(" />");
            }
            xml.AppendFormat("</categories>");

       
            // Add the sets
            foreach(var dataset in DataSets)
            {
                xml.AppendFormat("<dataset ");
                xml.AppendFormat("seriesName='{0}' ",       dataset.SeriesName);
                xml.AppendFormat(">");

                foreach(var set in dataset.Sets)
                {
                    xml.AppendFormat("<set ");
                    xml.AppendFormat("label='{0}' ",        set.Label);
                    xml.AppendFormat("value='{0}' ",        set.Value);
                    xml.AppendFormat("toolHelp='{0}' ",     set.ToolHelp);
                    xml.AppendFormat(" />");

                    if(set.VerticalTrendLine != null)
                    {
                        xml.AppendFormat(set.VerticalTrendLine.ToString());
                    }
                }

                xml.AppendFormat("</dataset>");
            }


            // Add any horizontal trend lines
            if(HorizontalTrendLines.Count > 0)
            {
                xml.AppendFormat("<trendLines>");
                foreach(var trendLine in HorizontalTrendLines)
                {
                    xml.AppendFormat(trendLine.ToString());
                }
                xml.AppendFormat("</trendLines>");
            }


            // End the chart tag
            xml.AppendFormat("</chart>");

            return xml.ToString();
        }
        #endregion

        #region Methods
        public void AddCategory(string category)
        {
            if(this.Categories.Where(c => c.Label == category).Count() == 0)
            {
                this.Categories.Add(new CategorySetDetail
                {
                    Label = category
                });
            }
        }
        public void AddSeries(string seriesName, decimal value)
        {
            var dataset = this.DataSets.Where(c => c.SeriesName == seriesName).FirstOrDefault();
                if(dataset == null) 
                {
                    dataset = new ChartDataSet() { SeriesName = seriesName };
                    this.DataSets.Add(dataset);
                }
                dataset.Sets.Add(new ChartDataSetDetail() 
                {
                    Value = value
                });
        }
        #endregion
    }
}