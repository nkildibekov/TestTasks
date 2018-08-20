using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace AdminDashboard.Models.FusionCharts
{
    public class VerticalTrendLine
    {
        public VerticalTrendLine()
        {

        }

        public decimal LinePosition                     = 0M;                       // (0-1) Position of the line between the 2 data points
        public string Label                             = string.Empty;             // Label of the vertical line
        public decimal LabelPosition                    = 0M;                       // (0-1) Position of the label (ranging from top of canvas to bottom)
        public bool ShowLabelBorder                     = true;                     // Whether to show border for this vertical line label
        public string LabelHorizontalAlignment          = "center";                 // (left, center, right) Horizontal anchor point for vLine label
        public string LabelVerticalAlignment            = "top";                    // (top, middle, bottom) Vertical anchor point for vLine label
        public string LineColor                         = "999999";                 // This attribute defines the color of vertical separator line
        public int LineThickness                        = 1;                        // Thickness in pixels of the vertical separator line
        public decimal LineOpacity                      = 100M;                     // (0-100) Alpha of the vertical separator line
        public bool DashedLine                          = false;                    // Is the line dashed?
        public int DashLength                           = 1;                        // The length of each dash in the line.
        public int DashGap                              = 1;                        // The space in between each gap of the dashed line.

        public override string ToString()
        {
            StringBuilder xml = new StringBuilder();


            xml.AppendFormat("<vLine ");
            xml.AppendFormat("label='{0}' ",            Label);
            xml.AppendFormat("showLabelBorder='{0}' ",  Convert.ToInt32(ShowLabelBorder));
            xml.AppendFormat("linePosition='{0}' ",     LinePosition);
            xml.AppendFormat("labelPosition='{0}' ",    LabelPosition);
            xml.AppendFormat("labelHAlign='{0}' ",      LabelHorizontalAlignment);
            xml.AppendFormat("labelVAlign='{0}' ",      LabelVerticalAlignment);
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