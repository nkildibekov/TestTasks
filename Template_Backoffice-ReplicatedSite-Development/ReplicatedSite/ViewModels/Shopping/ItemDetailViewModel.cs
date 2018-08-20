using ReplicatedSite.Models;
using ExigoService;

namespace ReplicatedSite.ViewModels
{
    public class ItemDetailViewModel : IShoppingViewModel
    {
        public IItem Item { get; set; }

        public ShoppingCartCheckoutPropertyBag PropertyBag { get; set; }
        public string[] Errors { get; set; }
    }
}