using Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public enum SummaryReportRevenueType
    {
        [Description("Total Billed")]
        Total = 1,

        [Description("Product/Supplies/Service Charges")]
        Subtotal = 2,

        [Description("Taxes")]
        Tax = 3,

        [Description("Shipping")]
        Shipping = 4
    }
}