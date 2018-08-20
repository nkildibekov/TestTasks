using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public class ProductSalesViewModel
    {
        public IEnumerable<ProductSalesRecord> ProductSales { get; set; }
    }

    public class ProductSalesRecord
    {
        public int WarehouseID { get; set; }
        public string CurrencyCode { get; set; }
        public string WarehouseDescription { get; set; }
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public decimal Total { get; set; }
    }
}