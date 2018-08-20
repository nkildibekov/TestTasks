using Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public enum SummaryReportType
    {
        [Description("Transactions")]
        TransactionCounts = 1,

        [Description("$MXP")]
        Revenue = 2
    }
}