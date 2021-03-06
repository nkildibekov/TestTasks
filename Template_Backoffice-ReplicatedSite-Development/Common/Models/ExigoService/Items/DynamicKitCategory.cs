﻿using System.Collections.Generic;

namespace ExigoService
{
    public class DynamicKitCategory : IDynamicKitCategory
    {
        public DynamicKitCategory()
        {
            this.Items = new List<DynamicKitCategoryItem>();
        }
        public int DynamicKitCategoryID { get; set; }
        public string DynamicKitCategoryDescription { get; set; }
        public decimal Quantity { get; set; }

        public List<DynamicKitCategoryItem> Items { get; set; }
    }
}