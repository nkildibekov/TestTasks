using Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public enum SummaryReportPaymentType
    {
        [Description("Cash at the Office")]
        Cash = 0,

        [Description("Credit Cards")]
        CreditCard = 1,

        [Description("Credits")]
        UseCredit = 6,

        [Description("Cash (Banamex)")]
        BanamexCash = 14,

        [Description("Cash (Bancomer)")]
        BancomerCash = 15,

        [Description("Bank Transfer")]
        BankTransfer = 16
    }
}