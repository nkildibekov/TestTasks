using System.Collections.Generic;

namespace ExigoService
{
    public interface IDynamicKitCategory
    {
        int DynamicKitCategoryID { get; set; }
        string DynamicKitCategoryDescription { get; set; }
        decimal Quantity { get; set; }

        List<DynamicKitCategoryItem> Items { get; set; }
    }
}