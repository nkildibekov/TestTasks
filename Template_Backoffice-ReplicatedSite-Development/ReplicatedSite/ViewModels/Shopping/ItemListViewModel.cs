﻿using ReplicatedSite.Models;
using ExigoService;
using System.Collections.Generic;

namespace ReplicatedSite.ViewModels
{
    public class ItemListViewModel : IShoppingViewModel
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public IEnumerable<IItem> Items { get; set; }
        public IEnumerable<ItemCategory> Categories { get; set; }
        public int Page { get; set; }
        public int RecordCount { get; set; }
        public ShoppingCartCheckoutPropertyBag PropertyBag { get; set; }
        public string[] Errors { get; set; }
    }
}