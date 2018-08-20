using ReplicatedSite.Models;
using ExigoService;
using System.Collections.Generic;

namespace ReplicatedSite.ViewModels
{
    public class PaymentMethodsViewModel : IShoppingViewModel
    {
        public IEnumerable<IPaymentMethod> PaymentMethods { get; set; }
        public IEnumerable<Address> Addresses { get; set; }
        public ShoppingCartCheckoutPropertyBag PropertyBag { get; set; }
        public string[] Errors { get; set; }
    }
}