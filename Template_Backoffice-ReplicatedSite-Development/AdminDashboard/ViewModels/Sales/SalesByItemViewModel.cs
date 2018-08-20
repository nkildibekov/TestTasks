using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdminDashboard.ViewModels
{
    public class ItemSalesByCountryCollection
    {
        public ItemSalesByCountryCollection()
        {
            this.Categories = new List<ItemSalesByCategoryCollection>();
        }

        public string Country { get; set; }
        public List<ItemSalesByCategoryCollection> Categories { get; set; }
    }

    public class ItemSalesByCategoryCollection
    {
        public ItemSalesByCategoryCollection()
        {
            this.Sales = new List<ItemSalesRecord>();
            this.Refunds = new List<ItemSalesRecord>();
        }

        public string Category { get; set; }
        public List<ItemSalesRecord> Sales { get; set; }
        public List<ItemSalesRecord> Refunds { get; set; }
    }

    public class ItemSalesRecord
    {
        public string Country { get; set; }
        public string ItemDescription { get; set; }
        public string ItemCode { get; set; }

        public decimal QuantityPerDay { get; set; }
        public decimal AmountPerDay { get; set; }
        public decimal TaxPerDay { get; set; }
        public decimal TotalPerDay { get; set; }

        public decimal QuantityPerMonth { get; set; }
        public decimal AmountPerMonth { get; set; }
        public decimal TaxPerMonth { get; set; }
        public decimal TotalPerMonth { get; set; }

        public decimal QuantityPerYear { get; set; }
        public decimal AmountPerYear { get; set; }
        public decimal TaxPerYear { get; set; }
        public decimal TotalPerYear { get; set; }

        public decimal QuantityPerLastYear { get; set; }
        public decimal AmountPerLastYear { get; set; }
        public decimal TaxPerLastYear { get; set; }
        public decimal TotalPerLastYear { get; set; }
    }

    public class WebCategoryItem
    {
        public string Category { get; set; }
        public string ItemCode { get; set; }
    }
}