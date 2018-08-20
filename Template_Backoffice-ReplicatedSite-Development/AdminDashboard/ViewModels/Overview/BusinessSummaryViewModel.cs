using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public class BusinessSummaryViewModel
    {
        public decimal AcceptedOrdersTotal { get; set; }
        public decimal CCPendingOrdersTotal { get; set; }
        public decimal ACHPendingOrdersTotal { get; set; }
        public decimal PendingAutoshipsTotal { get; set; }
        public decimal DeclinedOrdersTotal { get; set; }
        public decimal CancelledOrdersTotal { get; set; }
        public decimal AcceptedOrdersCount { get; set; }
        public decimal PrintedOrdersCount { get; set; }
        public decimal ShippedOrdersCount { get; set; }
        public decimal PendingOrdersCount { get; set; }
        public decimal CCPendingOrdersCount { get; set; }
        public decimal ACHPendingOrdersCount { get; set; }
        public decimal IncompleteOrdersCount { get; set; }
        public decimal ACHDeclinedOrdersCount { get; set; }
        public decimal CCDeclinedOrdersCount { get; set; }
        public decimal CancelledOrdersCount { get; set; }
    }
}