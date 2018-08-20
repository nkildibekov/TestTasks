using Common;
using Common.Api.ExigoWebService;
using Common.Providers;
using Dapper;
using ExigoService;
using ReplicatedSite.Factories;
using ReplicatedSite.Models;
using ReplicatedSite.Providers;
using ReplicatedSite.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ReplicatedSite.Controllers
{
    [RoutePrefix("{webalias}")]
    public class ShoppingController : Controller
    {
        #region Properties
        public string ShoppingCartName = GlobalSettings.Globalization.CookieKey + "ReplicatedSiteShopping";

        public ShoppingCartItemsPropertyBag ShoppingCart
        {
            get
            {
                if (_shoppingCart == null)
                {
                    _shoppingCart = Exigo.PropertyBags.Get<ShoppingCartItemsPropertyBag>(ShoppingCartName + "Cart");
                }
                return _shoppingCart;
            }
        }
        private ShoppingCartItemsPropertyBag _shoppingCart;

        public ShoppingCartCheckoutPropertyBag PropertyBag
        {
            get
            {
                if (_propertyBag == null)
                {
                    _propertyBag = Exigo.PropertyBags.Get<ShoppingCartCheckoutPropertyBag>(ShoppingCartName + "PropertyBag");
                }
                return _propertyBag;
            }
        }
        private ShoppingCartCheckoutPropertyBag _propertyBag;

        public IOrderConfiguration OrderConfiguration = (Identity.Customer == null) ? Identity.Owner.Market.Configuration.Orders : Identity.Customer.Market.Configuration.Orders;

        public ILogicProvider LogicProvider
        {
            get
            {
                if (_logicProvider == null)
                {
                    _logicProvider = new ShoppingCartLogicProvider(this, ShoppingCart, PropertyBag);
                }
                return _logicProvider;
            }
        }
        private ILogicProvider _logicProvider;
        #endregion

        #region Items/Cart
       

        [Route("products")]
        public ActionResult ItemList()
        {
            var model = ShoppingViewModelFactory.Create<ItemListViewModel>(PropertyBag);

            model.Categories = Exigo.GetWebCategoriesRecursively(OrderConfiguration.CategoryID);

            return View(model);
        }

        [Route("product/{itemcode}")]
        public ActionResult ItemDetail(string itemcode)
        {
            var model = ShoppingViewModelFactory.Create<ItemDetailViewModel>(PropertyBag);
            var languageID = Exigo.GetSelectedLanguageID();

            model.Item = Exigo.GetItems(new ExigoService.GetItemsRequest
            {
                Configuration = OrderConfiguration,
                ItemCodes = new List<string> { itemcode }.ToArray(),
                LanguageID = languageID
            }).FirstOrDefault();

            if (model.Item != null)
            {
                model.Item.Quantity = 1;
            }

            return View(model);
        }

        [Route("cart")]
        public ActionResult Cart()
        {
            var model = ShoppingViewModelFactory.Create<CartViewModel>(PropertyBag);
            var languageID = Exigo.GetSelectedLanguageID();

            // Get the cart items
            var cartItems = ShoppingCart.Items.ToList();
            model.Items = Exigo.GetItems(cartItems, OrderConfiguration, languageID).ToList();

            return View(model);
        }
        #endregion

        #region Shipping
        [Route("checkout/shipping")]
        public ActionResult Shipping()
        {
            var model = ShoppingViewModelFactory.Create<ShippingAddressesViewModel>(PropertyBag);

            model.Addresses = Exigo.GetCustomerAddresses(Identity.Customer.CustomerID)
                .Where(c => c.IsComplete)
                .Select(c => c as ShippingAddress);


            return View(model);
        }

        [HttpPost]
        [Route("checkout/shipping")]
        public ActionResult Shipping(ShippingAddress address)
        {
            // Attempt to validate the user's entered address if US address
            address = GlobalUtilities.ValidateAddress(address) as ShippingAddress;

            // Save the address to the customer's account if applicable
            if (Request.IsAuthenticated && address.AddressType == AddressType.New)
            {
                Exigo.SetCustomerAddressOnFile(Identity.Customer.CustomerID, address as Address);
            }

            PropertyBag.ShippingAddress = address;
            Exigo.PropertyBags.Update(PropertyBag);

            return LogicProvider.GetNextAction();

        }
        #endregion

        #region Payments
        [Route("checkout/payment")]
        public ActionResult Payment()
        {
            var model = ShoppingViewModelFactory.Create<PaymentMethodsViewModel>(PropertyBag);

            model.PaymentMethods = Exigo.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
            {
                CustomerID = Identity.Customer.CustomerID,
                ExcludeIncompleteMethods = true,
                ExcludeInvalidMethods = true
            });

            model.Addresses = Exigo.GetCustomerAddresses(Identity.Customer.CustomerID)
                .Where(c => c.IsComplete)
                .Select(c => c as ShippingAddress);

            return View("Payment", model);
        }

        [HttpPost]
        public ActionResult UseCreditCardOnFile(CreditCardType type)
        {
            var paymentMethod = Exigo.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
            {
                CustomerID = Identity.Customer.CustomerID,
                ExcludeIncompleteMethods = true,
                ExcludeInvalidMethods = true
            }).Where(c => c is CreditCard && ((CreditCard)c).Type == type).FirstOrDefault();

            return UsePaymentMethod(paymentMethod);
        }

        [HttpPost]
        public ActionResult UseBankAccountOnFile(ExigoService.BankAccountType type)
        {
            var paymentMethod = Exigo.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
            {
                CustomerID = Identity.Customer.CustomerID,
                ExcludeIncompleteMethods = true,
                ExcludeInvalidMethods = true
            }).Where(c => c is BankAccount && ((BankAccount)c).Type == type).FirstOrDefault();

            return UsePaymentMethod(paymentMethod);
        }

        [HttpPost]
        public ActionResult UseCreditCard(CreditCard newCard, bool billingSameAsShipping = false)
        {
            if (billingSameAsShipping)
            {
                var address = PropertyBag.ShippingAddress;

                newCard.BillingAddress = new Address
                {
                    Address1 = address.Address1,
                    Address2 = address.Address2,
                    City = address.City,
                    State = address.State,
                    Zip = address.Zip,
                    Country = address.Country
                };
            }


            // Verify that the card is valid
            if (!newCard.IsValid)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    message = "This card is invalid, please try again"
                });
            }
            else
            {
                // Use if you want to have cards auto save on the Customer's account when added
                #region Save Cards if new
                //if (Identity.Customer != null)
                //{
                //    // Save the credit card to the customer's account if applicable                
                //    var paymentMethodsOnFile = Exigo.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
                //    {
                //        CustomerID = Identity.Customer.CustomerID,
                //        ExcludeIncompleteMethods = true,
                //        ExcludeInvalidMethods = true
                //    }).Where(c => c is CreditCard).Select(c => c as CreditCard);

                //    if (paymentMethodsOnFile.Where(c => c.Type == CreditCardType.Primary).FirstOrDefault() == null)
                //    {
                //        Exigo.SetCustomerCreditCard(Identity.Customer.CustomerID, newCard, CreditCardType.Primary);
                //    }
                //    else if (paymentMethodsOnFile.Where(c => c.Type == CreditCardType.Secondary).FirstOrDefault() == null)
                //    {
                //        Exigo.SetCustomerCreditCard(Identity.Customer.CustomerID, newCard, CreditCardType.Secondary);
                //    }
                //}
                #endregion


                return UsePaymentMethod(newCard);
            }
        }

        [HttpPost]
        public ActionResult UseBankAccount(BankAccount newBankAccount, bool billingSameAsShipping = false)
        {
            if (billingSameAsShipping)
            {
                var address = PropertyBag.ShippingAddress;

                newBankAccount.BillingAddress = new Address
                {
                    Address1 = address.Address1,
                    Address2 = address.Address2,
                    City = address.City,
                    State = address.State,
                    Zip = address.Zip,
                    Country = address.Country
                };
            }

            // Verify that the card is valid
            if (!newBankAccount.IsValid)
            {
                return new JsonNetResult(new
                {
                    success = false
                });
            }
            else
            {
                // Save the bank account to the customer's account if applicable                
                var paymentMethodsOnFile = Exigo.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
                {
                    CustomerID = Identity.Customer.CustomerID,
                    ExcludeIncompleteMethods = true,
                    ExcludeInvalidMethods = true,
                }).Where(c => c is BankAccount).Select(c => c as BankAccount);

                if (paymentMethodsOnFile.FirstOrDefault() == null)
                {
                    Exigo.SetCustomerBankAccount(Identity.Customer.CustomerID, newBankAccount);
                }
            }

            return UsePaymentMethod(newBankAccount);
        }

        [HttpPost]
        public ActionResult UsePaymentMethod(IPaymentMethod paymentMethod)
        {
            try
            {
                PropertyBag.PaymentMethod = paymentMethod;
                Exigo.PropertyBags.Update(PropertyBag);

                return new JsonNetResult(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        #endregion

        #region Review/Checkout
        [Route("checkout/review")]
        public ActionResult Review()
        {
            var logicResult = LogicProvider.CheckLogic();
            if (!logicResult.IsValid) return logicResult.NextAction;

            var model = ShoppingViewModelFactory.Create<OrderReviewViewModel>(PropertyBag);
            var languageID = Exigo.GetSelectedLanguageID();

            #region Order Totals
            var beginningShipMethodID = PropertyBag.ShipMethodID;

            // If this is the first time coming to the page, and the property bag's ship method has not been set, then set it to the default for the configuration
            if (PropertyBag.ShipMethodID == 0)
            {
                PropertyBag.ShipMethodID = OrderConfiguration.DefaultShipMethodID;
                beginningShipMethodID = PropertyBag.ShipMethodID;
                Exigo.PropertyBags.Update(PropertyBag);
            }


            // Get the cart items
            var cartItems = ShoppingCart.Items.ToList();
            model.Items = Exigo.GetItems(cartItems, OrderConfiguration, languageID).ToList();

            model.OrderTotals = Exigo.CalculateOrder(new OrderCalculationRequest
            {
                Configuration = OrderConfiguration,
                Items = cartItems,
                Address = PropertyBag.ShippingAddress,
                ShipMethodID = PropertyBag.ShipMethodID,
                ReturnShipMethods = true
            });
            model.ShipMethods = model.OrderTotals.ShipMethods;

            // Set the default ship method
            if (model.ShipMethods.Count() > 0)
            {
                if (model.ShipMethods.Any(c => c.ShipMethodID == PropertyBag.ShipMethodID))
                {
                    // If the property bag ship method ID exists in the results from order calc, set the correct result as selected                
                    model.ShipMethods.First(c => c.ShipMethodID == PropertyBag.ShipMethodID).Selected = true;
                }
                else
                {
                    // If we don't have the ship method we're supposed to select, check the first one, save the selection and recalculate
                    model.ShipMethods.First().Selected = true;

                    // If for some reason the property bag is outdated and the ship method stored in it is not in the list, set the first result as selected and re-set the property bag's value
                    PropertyBag.ShipMethodID = model.ShipMethods.FirstOrDefault().ShipMethodID;
                    Exigo.PropertyBags.Update(PropertyBag);
                }
            }

            // If the original property bag value has changed from the beginning of the call, re-calculate the values
            if (beginningShipMethodID != PropertyBag.ShipMethodID)
            {
                var newCalculationResult = Exigo.CalculateOrder(new OrderCalculationRequest
                {
                    Configuration = OrderConfiguration,
                    Items = cartItems,
                    Address = PropertyBag.ShippingAddress,
                    ShipMethodID = PropertyBag.ShipMethodID,
                    ReturnShipMethods = false,
                    CustomerID = Identity.Customer.CustomerID
                });

                model.OrderTotals = newCalculationResult;
            }
            #endregion

            return View(model);
        }

        [HttpPost]
        public ActionResult SubmitCheckout()
        {
            if (!PropertyBag.IsSubmitting)
            {
                PropertyBag.IsSubmitting = true;
                _propertyBag = Exigo.PropertyBags.Update(PropertyBag);

                try
                {
                    // Start creating the API requests
                    var details = new List<ApiRequest>();
                    var orderRequest = new CreateOrderRequest(OrderConfiguration, PropertyBag.ShipMethodID, ShoppingCart.Items, PropertyBag.ShippingAddress)
                    {
                        CustomerID = Identity.Customer.CustomerID
                    };

                    details.Add(orderRequest);

                    // Create the payment request
                    if (PropertyBag.PaymentMethod is CreditCard)
                    {
                        var card = PropertyBag.PaymentMethod as CreditCard;
                        if (card.Type == CreditCardType.New)
                        {
                            if (!card.IsTestCreditCard && !Request.IsLocal)
                            {
                                details.Add(new ChargeCreditCardTokenRequest(card));
                            }
                            else
                            {
                                // Test Credit Card, so no need to charge card
                                ((CreateOrderRequest)details.Where(c => c is CreateOrderRequest).FirstOrDefault()).OrderStatus = GlobalUtilities.GetDefaultOrderStatusType();
                            }
                        }
                        else
                        {
                            details.Add(new ChargeCreditCardTokenOnFileRequest(card));
                        }
                    }
                    if (PropertyBag.PaymentMethod is BankAccount)
                    {
                        var account = PropertyBag.PaymentMethod as BankAccount;
                        if (account.Type == ExigoService.BankAccountType.New)
                        {
                            details.Add(new DebitBankAccountRequest(account));
                        }
                        else
                        {
                            details.Add(new DebitBankAccountOnFileRequest(account));
                        }
                    }


                    // Process the transaction
                    var transactionRequest = new TransactionalRequest();
                    transactionRequest.TransactionRequests = details.ToArray();
                    var transactionResponse = Exigo.WebService().ProcessTransaction(transactionRequest);


                    var newOrderID = 0;
                    if (transactionResponse.Result.Status == ResultStatus.Success)
                    {
                        foreach (var response in transactionResponse.TransactionResponses)
                        {
                            if (response is CreateOrderResponse)
                            {
                                var orderResponse = (CreateOrderResponse)response;
                                newOrderID = orderResponse.OrderID;

                                // Create a cookie to store our newest Order ID to ensure it shows on the Order History page
                                var orderIDCookie = new System.Web.HttpCookie("NewOrder_{0}".FormatWith(Identity.Customer.CustomerID), newOrderID.ToString());
                                orderIDCookie.Expires = DateTime.UtcNow.AddMinutes(5);
                                Response.Cookies.Add(orderIDCookie);
                            }
                        }
                    }

                    PropertyBag.NewOrderID = newOrderID;
                    _propertyBag = Exigo.PropertyBags.Update(PropertyBag);

                    return new JsonNetResult(new
                    {
                        success = true
                    });
                }
                catch (Exception exception)
                {
                    PropertyBag.OrderException = exception.Message;
                    PropertyBag.IsSubmitting = false;
                    _propertyBag = Exigo.PropertyBags.Update(PropertyBag);

                    return new JsonNetResult(new
                    {
                        success = false,
                        message = exception.Message
                    });
                }
            }
            else
            {
                if (PropertyBag.NewOrderID > 0)
                {
                    return new JsonNetResult(new
                    {
                        success = true
                    });
                }
                else
                {
                    return new JsonNetResult(new
                    {
                        success = false,
                        message = Resources.Common.YourOrderIsSubmitting
                    });
                }
            }
        }

        [Route("thank-you")]
        public ActionResult OrderComplete()
        {
            Exigo.PropertyBags.Delete(PropertyBag);
            Exigo.PropertyBags.Delete(ShoppingCart);
            return View();
        }

        [Route("checkout")]
        public ActionResult Checkout()
        {
            return LogicProvider.GetNextAction();
        }
        #endregion

        #region Ajax
        [HttpPost]
        public JsonNetResult GetItemList(int categoryID = 0)
        {
            try
            {
                var items = new List<Item>();
                var newItems = new List<Item>();

                ExigoService.GetItemsRequest itemsRequest;

                var defaultCategoryID = (categoryID == 0) ? OrderConfiguration.CategoryID : categoryID;
                var categories = Exigo.GetWebCategoriesRecursively(defaultCategoryID).OrderBy(c => c.SortOrder);

                if (categories != null && categories.Count() > 0)
                {
                    var featuredCategoryID = OrderConfiguration.FeaturedCategoryID.Equals(0) ? OrderConfiguration.CategoryID : OrderConfiguration.FeaturedCategoryID;
                    var category = categories.Where(c => c.ParentID == featuredCategoryID).FirstOrDefault();

                    itemsRequest = new ExigoService.GetItemsRequest
                    {
                        Configuration = OrderConfiguration,
                        IncludeChildCategories = true,
                        CategoryID = category.WebCategoryID
                    };

                    newItems = Exigo.GetItems(itemsRequest).OrderBy(c => c.SortOrder).ToList();

                    foreach (var newItem in newItems)
                    {
                        if (items.Count(i => i.ItemCode == newItem.ItemCode).Equals(0))
                        {
                            items.Add(newItem);
                        }
                    }
                }
                else
                {
                    itemsRequest = new ExigoService.GetItemsRequest
                    {
                        Configuration = OrderConfiguration,
                        IncludeChildCategories = true,
                        CategoryID = categoryID
                    };

                    items = Exigo.GetItems(itemsRequest).OrderBy(c => c.SortOrder).ToList();
                }

                var html = this.RenderPartialViewToString("Partials/Items/_ShoppingItemList", items);

                return new JsonNetResult(new
                {
                    success = true,
                    html = html
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public JsonNetResult CheckOrderSubmissionStatus()
        {
            if (PropertyBag.NewOrderID > 0)
            {
                return new JsonNetResult(new
                {
                    success = true
                });
            }
            else
            {
                var orderHadException = (PropertyBag.IsSubmitting == false);

                return new JsonNetResult(new
                {
                    success = false,
                    orderHadException = orderHadException,
                    exceptionMessage = PropertyBag.OrderException
                });
            }
        }

        [HttpPost]
        public ActionResult AddItemToCart(Item item)
        {
            ShoppingCart.Items.Add(item);
            Exigo.PropertyBags.Update(ShoppingCart);

            // Return the result
            if (Request.IsAjaxRequest())
            {
                return new JsonNetResult(new
                {
                    success = true
                });
            }
            else
            {
                return RedirectToAction("Cart");
            }
        }

        [HttpPost]
        public ActionResult RemoveItemFromCart(Guid id)
        {
            var item = ShoppingCart.Items.Where(c => c.ID == id).FirstOrDefault();
            var subtotal = 0M;

            ShoppingCart.Items.Remove(id);
            Exigo.PropertyBags.Update(ShoppingCart);

            if (ShoppingCart.Items.Count() > 0)
            {
                var itemCodes = ShoppingCart.Items.Select(c => c.ItemCode);
                var items = Exigo.GetItems(new ExigoService.GetItemsRequest
                {
                    Configuration = OrderConfiguration,
                    ItemCodes = itemCodes.ToArray()
                }).ToList();

                foreach (var cartItem in items)
                {
                    var shoppingCartItem = ShoppingCart.Items.Where(c => c.ItemCode == cartItem.ItemCode).FirstOrDefault();
                    cartItem.Quantity = shoppingCartItem.Quantity;
                }

                subtotal = items.Sum(c => c.Quantity * c.Price);
            }

            if (Request.IsAjaxRequest())
            {
                return new JsonNetResult(new
                {
                    success = true,
                    item = item,
                    items = ShoppingCart.Items,
                    subtotal = subtotal
                });
            }
            else
            {
                return RedirectToAction("Cart");
            }
        }

        [HttpPost]
        public ActionResult UpdateItemQuantity(Guid id, decimal quantity)
        {
            var item = ShoppingCart.Items.Where(c => c.ID == id).FirstOrDefault();
            var subtotal = 0M;

            ShoppingCart.Items.Update(id, quantity);
            Exigo.PropertyBags.Update(ShoppingCart);

            if (quantity == 0)
            {
                item.Quantity = 0;
            }


            if (ShoppingCart.Items.Count() > 0)
            {
                var itemCodes = ShoppingCart.Items.Select(c => c.ItemCode);
                var items = Exigo.GetItems(new ExigoService.GetItemsRequest
                {
                    Configuration = OrderConfiguration,
                    ItemCodes = itemCodes.ToArray()
                }).ToList();

                foreach (var cartItem in items)
                {
                    var shoppingCartItem = ShoppingCart.Items.Where(c => c.ItemCode == cartItem.ItemCode).FirstOrDefault();
                    cartItem.Quantity = shoppingCartItem.Quantity;
                }

                subtotal = items.Sum(c => c.Quantity * c.Price);
            }


            if (Request.IsAjaxRequest())
            {
                return new JsonNetResult(new
                {
                    success = true,
                    item = item,
                    items = ShoppingCart.Items,
                    subtotal = subtotal
                });
            }
            else
            {
                return RedirectToAction("Cart");
            }
        }

        [HttpPost]
        public ActionResult SetShipMethodID(int shipMethodID)
        {
            PropertyBag.ShipMethodID = shipMethodID;
            Exigo.PropertyBags.Update(PropertyBag);

            return RedirectToAction("Review");
        }

        [HttpPost]
        public JsonNetResult SetNoReferred()
        {
            try
            {
                PropertyBag.SelectedDistributor = "NoReferred";
                Exigo.PropertyBags.Update(PropertyBag);

                return new JsonNetResult(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        #endregion
    }
}