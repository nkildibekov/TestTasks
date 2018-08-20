using Backoffice.Models;
using ExigoService;
using System.Collections.Generic;

namespace Backoffice.ViewModels
{
    public class CartViewModel : IShoppingViewModel
    {
        public IEnumerable<IItem> Items { get; set; }

        public ShoppingCartCheckoutPropertyBag PropertyBag { get; set; }
        public string[] Errors { get; set; }
    }
}