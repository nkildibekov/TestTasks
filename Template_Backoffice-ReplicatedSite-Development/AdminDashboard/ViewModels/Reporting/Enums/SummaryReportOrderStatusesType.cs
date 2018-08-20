using Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public enum SummaryReportOrderStatusesType
    {
        [Description("Gross")]
        All = 1,

        [Description("Paid")]
        Accepted = 2,

        [Description("Pending")]
        Pending = 3,

        [Description("Cancelled")]
        Cancelled = 4
    }
}