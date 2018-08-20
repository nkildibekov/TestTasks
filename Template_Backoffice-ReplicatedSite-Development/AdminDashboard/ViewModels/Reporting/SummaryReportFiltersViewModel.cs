using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public class SummaryReportFiltersViewModel
    {
        public SummaryReportFiltersViewModel()
        {
            this.OrderStatusesType = SummaryReportOrderStatusesType.All;
            this.ReportType        = SummaryReportType.TransactionCounts;
            this.PaymentTypes      = new List<SummaryReportPaymentType>();
            this.RevenueType       = SummaryReportRevenueType.Total;
        }

        [Required, DataType("Date"), Display(Name = "From", ResourceType = typeof(Common.Resources.Models))]
        public DateTime StartDate { get; set; }

        [Required, DataType("Date"), Display(Name = "To", ResourceType = typeof(Common.Resources.Models))]
        public DateTime EndDate { get; set; }

        public SummaryReportType ReportType { get; set; }
        public SummaryReportOrderStatusesType OrderStatusesType { get; set; }
        public List<SummaryReportPaymentType> PaymentTypes { get; set; }
        public SummaryReportRevenueType RevenueType { get; set; }

        public List<int> PaymentTypeIDs
        {
            get
            {
                return PaymentTypes.Select(c => (int)c).ToList();
            }
        }
        public List<int> AutoOrderPaymentTypeIDs
        {
            get
            {
                var results = new List<int>();

                foreach(var type in PaymentTypes)
                {
                    switch (type)
                    {
                        case SummaryReportPaymentType.CreditCard: 
                            results.Add(1); break;

                        case SummaryReportPaymentType.Cash:
                        case SummaryReportPaymentType.BanamexCash:
                        case SummaryReportPaymentType.BancomerCash:
                            results.Add(4); break;

                        case SummaryReportPaymentType.BankTransfer:
                            results.Add(3); break;
                    }
                }

                return results.Distinct().ToList();
            }
        }
    }
}