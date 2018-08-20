using System;
using ExigoService;
using Common.Api.ExigoWebService;

namespace ReplicatedSite.Models
{
    public class ShoppingCartCheckoutPropertyBag : BasePropertyBag
    {
        private string version = "1.0.1";
        private int expires = 31;

        #region Constructors
        public ShoppingCartCheckoutPropertyBag()
        {
            CustomerID = (Identity.Customer != null) ? Identity.Customer.CustomerID : 0;            
            Expires = expires;
            ShippingAddress = new ShippingAddress();
        }
        #endregion

        #region Shared Properties
        public int CustomerID { get; set; }        
        public ShippingAddress ShippingAddress { get; set; }        
        public int ShipMethodID { get; set; }       
        public IPaymentMethod PaymentMethod { get; set; }
        public bool IsSubmitting { get; set; }
        public int NewOrderID { get; set; }
        public string OrderException { get; set; }
        public string SelectedDistributor { get; set; }
        #endregion

        #region Shopping With Auto Order Only Properties
        public int NewAutoOrderID { get; set; }
        public FrequencyType AutoOrderFrequencyType { get; set; }
        public DateTime AutoOrderStartDate { get; set; }
        public ShippingAddress AutoOrderShippingAddress { get; set; }
        public ShippingAddress AutoOrderBillingAddress { get; set; }
        public IPaymentMethod AutoOrderPaymentMethod { get; set; }
        public bool AutoOrderBillingSameAsShipping { get; set; }
        public int AutoOrderShipMethodID { get; set; }
        #endregion

        #region Methods
        public override T OnBeforeUpdate<T>(T propertyBag)
        {
            propertyBag.Version = version;

            return propertyBag;
        }
        public override bool IsValid()
        {
            var currentCustomerID = (Identity.Customer != null) ? Identity.Customer.CustomerID : 0;
            return this.Version == version && this.CustomerID == currentCustomerID;
        }
        #endregion
    }
}