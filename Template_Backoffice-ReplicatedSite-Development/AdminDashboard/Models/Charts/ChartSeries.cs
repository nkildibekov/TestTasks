using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class ChartSeries
{
    public ChartSeries()
    {
        Values = new List<decimal>();
    }

    #region Properties
    public string SeriesName { get; set; }
    public List<decimal> Values { get; set; }
    #endregion
}