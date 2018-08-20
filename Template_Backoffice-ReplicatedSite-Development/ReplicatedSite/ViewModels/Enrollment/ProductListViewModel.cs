using ExigoService;
using System.Collections.Generic;

namespace ReplicatedSite.ViewModels
{
    public class EnrollmentProductListViewModel
    {
        public IEnumerable<IItem> OrderItems { get; set; }
        public IEnumerable<IItem> AutoOrderItems { get; set; }        
    }
}