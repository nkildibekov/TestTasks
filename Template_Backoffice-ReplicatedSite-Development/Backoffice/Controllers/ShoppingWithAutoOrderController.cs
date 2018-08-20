using Backoffice.Factories;
using Backoffice.Models;
using Backoffice.Providers;
using Backoffice.ViewModels;
using Common;
using Common.Api.ExigoWebService;
using Common.Providers;
using ExigoService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Backoffice.Controllers
{
    [RoutePrefix("store")]
    public class ShoppingWithAutoOrderController : Controller
    {
        #region Properties
        public string ShoppingCartName = GlobalSettings.Globalization.CookieKey + "BackofficeShopping";

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

        public IOrderConfiguration OrderConfiguration = Identity.Current.Market.Configuration.BackOfficeOrders;
        public IOrderConfiguration AutoOrderConfiguration = Identity.Current.Market.Configuration.BackOfficeAutoOrders;

        public ILogicProvider LogicProvider
        {
            get
            {
                if (_logicProvider == null)
                {
                    _logicProvider = new ShoppingCartWithAutoOrderLogicProvider(this, ShoppingCart, PropertyBag);
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
            var configuration = OrderConfiguration;
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

            model.Addresses = Exigo.GetCustomerAddresses(Identity.Current.CustomerID)
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
                Exigo.SetCustomerAddressOnFile(Identity.Current.CustomerID, address as Address);
            }

            PropertyBag.ShippingAddress = address;
            Exigo.PropertyBags.Update(PropertyBag);

            return LogicProvider.GetNextAction();

        }
        #endregion

        #region AutoOrders
        [Route("checkout/autoorder")]
        public ActionResult AutoOrder()
        {
            var model = ShoppingViewModelFactory.Create<AutoOrderSettingsViewModel>(PropertyBag);

            // Ensure we have a valid frequency type
            if (!GlobalSettings.AutoOrders.AvailableFrequencyTypes.Contains(PropertyBag.AutoOrderFrequencyType))
            {
                PropertyBag.AutoOrderFrequencyType = GlobalSettings.AutoOrders.AvailableFrequencyTypes.FirstOrDefault();
            }

            // Ensure we have a valid start date based on the frequency
            if (PropertyBag.AutoOrderStartDate == DateTime.MinValue.ToCST())
            {
                PropertyBag.AutoOrderStartDate = GlobalUtilities.GetAutoOrderStartDate(PropertyBag.AutoOrderFrequencyType);
            }


            // Set our model
            model.AutoOrderStartDate = PropertyBag.AutoOrderStartDate;
            model.AutoOrderFrequencyType = PropertyBag.AutoOrderFrequencyType;

            return View(model);
        }

        [HttpPost]
        [Route("checkout/autoorder")]
        public ActionResult AutoOrder(DateTime autoOrderStartDate, FrequencyType autoOrderFrequencyType)
        {
            PropertyBag.AutoOrderStartDate = autoOrderStartDate;
            PropertyBag.AutoOrderFrequencyType = autoOrderFrequencyType;
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
                    CustomerID = Identity.Current.CustomerID,
                    ExcludeIncompleteMethods = true,
                    ExcludeInvalidMethods = true
                });
                model.Addresses = Exigo.GetCustomerAddresses(Identity.Current.CustomerID)
                    .Where(c => c.IsComplete)
                    .Select(c => c as ShippingAddress);

            ViewBag.HasAutoOrderItems = ShoppingCart.Items.Count(c => c.Type == ShoppingCartItemType.AutoOrder) > 0;

            return View("Payment", model);
        }

        [HttpPost]
        public ActionResult UseCreditCardOnFile(CreditCardType type)
        {
            var paymentMethod = Exigo.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
            {
                CustomerID = Identity.Current.CustomerID,
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
                CustomerID = Identity.Current.CustomerID,
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
                    message = Resources.Common.CardIsInvalid
                });
            }
            else
            {
                // Save the credit card to the customer's account if applicable
                //if (LogicProvider.IsAuthenticated())
                //{
                //    var paymentMethodsOnFile = Exigo.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
                //    {
                //        CustomerID = Identity.Current.CustomerID,
                //        ExcludeIncompleteMethods = true,
                //        ExcludeInvalidMethods = true
                //    }).Where(c => c is CreditCard).Select(c => c as CreditCard);

                //    if (paymentMethodsOnFile.Where(c => c.Type == CreditCardType.Primary).FirstOrDefault() == null)
                //    {
                //        Exigo.SetCustomerCreditCard(Identity.Current.CustomerID, newCard, CreditCardType.Primary);
                //    }
                //    else if (paymentMethodsOnFile.Where(c => c.Type == CreditCardType.Secondary).FirstOrDefault() == null)
                //    {
                //        Exigo.SetCustomerCreditCard(Identity.Current.CustomerID, newCard, CreditCardType.Secondary);
                //    }

                //    // Here we make sure that if the user is entering a new card that needs to be saved as a specific type, we handle that here - Mike M.
                //    if (newCard.Type != CreditCardType.New)
                //    {
                //        Exigo.SetCustomerCreditCard(Identity.Current.CustomerID, newCard, newCard.Type);
                //    }
                //}

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
                if (LogicProvider.IsAuthenticated())
                {
                    var paymentMethodsOnFile = Exigo.GetCustomerPaymentMethods(new GetCustomerPaymentMethodsRequest
                    {
                        CustomerID = Identity.Current.CustomerID,
                        ExcludeIncompleteMethods = true,
                        ExcludeInvalidMethods = true,
                        ExcludeNonAutoOrderPaymentMethods = true
                    }).Where(c => c is BankAccount).Select(c => c as BankAccount);

                    if (paymentMethodsOnFile.FirstOrDefault() == null)
                    {
                        Exigo.SetCustomerBankAccount(Identity.Current.CustomerID, newBankAccount);
                    }
                }


                return UsePaymentMethod(newBankAccount);
            }
        }

        [HttpPost]
        public ActionResult UsePaymentMethod(IPaymentMethod paymentMethod)
        {
            PropertyBag.PaymentMethod = paymentMethod;
            Exigo.PropertyBags.Update(PropertyBag);

            return new JsonNetResult(new
            {
                success = true
            });
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


            // Get the cart items
            var cartItems = ShoppingCart.Items.ToList();
            model.Items = Exigo.GetItems(cartItems, OrderConfiguration, languageID).ToList();


            // Calculate the order if applicable
            var orderItems = cartItems.Where(c => c.Type == ShoppingCartItemType.Order).ToList();
            if (orderItems.Count > 0)
            {
                #region Order Totals
                var beginningShipMethodID = PropertyBag.ShipMethodID;

                // If this is the first time coming to the page, and the property bag's ship method has not been set, then set it to the default for the configuration
                if (PropertyBag.ShipMethodID == 0)
                {
                    PropertyBag.ShipMethodID = OrderConfiguration.DefaultShipMethodID;
                    beginningShipMethodID = PropertyBag.ShipMethodID;
                    Exigo.PropertyBags.Update(PropertyBag);
                }

                model.OrderTotals = Exigo.CalculateOrder(new OrderCalculationRequest
                {
                    Configuration = OrderConfiguration,
                    Items = orderItems,
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
                        Items = orderItems,
                        Address = PropertyBag.ShippingAddress,
                        ShipMethodID = PropertyBag.ShipMethodID,
                        ReturnShipMethods = false,
                        CustomerID = Identity.Current.CustomerID
                    });

                    model.OrderTotals = newCalculationResult;
                }
                #endregion
            }


            // Calculate the autoorder if applicable
            var autoOrderItems = cartItems.Where(c => c.Type == ShoppingCartItemType.AutoOrder).ToList();
            if (autoOrderItems.Count > 0)
            {
                #region Auto Order Totals
                var beginningAutoOrderShipMethodID = PropertyBag.AutoOrderShipMethodID;

                // If this is the first time coming to the page, and the property bag's ship method has not been set, then set it to the default for the configuration
                if (PropertyBag.ShipMethodID == 0)
                {
                    PropertyBag.ShipMethodID = AutoOrderConfiguration.DefaultShipMethodID;
                    beginningAutoOrderShipMethodID = PropertyBag.AutoOrderShipMethodID;
                    Exigo.PropertyBags.Update(PropertyBag);
                }

                model.AutoOrderTotals = Exigo.CalculateOrder(new OrderCalculationRequest
                {
                    Configuration = AutoOrderConfiguration,
                    Items = autoOrderItems,
                    Address = PropertyBag.ShippingAddress,
                    ShipMethodID = PropertyBag.AutoOrderShipMethodID,
                    ReturnShipMethods = true
                });
                model.AutoOrderShipMethods = model.AutoOrderTotals.ShipMethods;

                // Set the default ship method
                if (model.AutoOrderShipMethods.Count() > 0)
                {
                    if (model.AutoOrderShipMethods.Any(c => c.ShipMethodID == PropertyBag.AutoOrderShipMethodID))
                    {
                        // If the property bag ship method ID exists in the results from order calc, set the correct result as selected                
                        model.AutoOrderShipMethods.First(c => c.ShipMethodID == PropertyBag.AutoOrderShipMethodID).Selected = true;
                    }
                    else
                    {
                        // If we don't have the ship method we're supposed to select, check the first one, save the selection and recalculate
                        model.AutoOrderShipMethods.First().Selected = true;

                        // If for some reason the property bag is outdated and the ship method stored in it is not in the list, set the first result as selected and re-set the property bag's value
                        PropertyBag.AutoOrderShipMethodID = model.AutoOrderShipMethods.FirstOrDefault().ShipMethodID;
                        Exigo.PropertyBags.Update(PropertyBag);
                    }
                }

                // If the original property bag value has changed from the beginning of the call, re-calculate the values
                if (beginningAutoOrderShipMethodID != 0 && beginningAutoOrderShipMethodID != PropertyBag.AutoOrderShipMethodID)
                {
                    var newCalculationResult = Exigo.CalculateOrder(new OrderCalculationRequest
                    {
                        Configuration = AutoOrderConfiguration,
                        Items = autoOrderItems,
                        Address = PropertyBag.ShippingAddress,
                        ShipMethodID = PropertyBag.AutoOrderShipMethodID,
                        ReturnShipMethods = true,
                        CustomerID = Identity.Current.CustomerID
                    });

                    model.AutoOrderTotals = newCalculationResult;
                    model.AutoOrderShipMethods = newCalculationResult.ShipMethods;
                }

                if (orderItems.Count == 0)
                {
                    model.ShipMethods = model.AutoOrderTotals.ShipMethods;

                    if (PropertyBag.ShipMethodID != PropertyBag.AutoOrderShipMethodID)
                    {
                        PropertyBag.ShipMethodID = PropertyBag.AutoOrderShipMethodID;
                        Exigo.PropertyBags.Update(PropertyBag);
                    }
                }
                #endregion
            }

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


                    // Create the order request, if applicable
                    var orderItems = ShoppingCart.Items.Where(c => c.Type == ShoppingCartItemType.Order);
                    var hasOrder = orderItems.Count() > 0;

                    if (hasOrder)
                    {
                        var orderRequest = new CreateOrderRequest(OrderConfiguration, PropertyBag.ShipMethodID, orderItems, PropertyBag.ShippingAddress)
                        {
                            CustomerID = Identity.Current.CustomerID
                        };
                        details.Add(orderRequest);
                    }


                    // Create the autoorder request, if applicable
                    var autoOrderItems = ShoppingCart.Items.Where(c => c.Type == ShoppingCartItemType.AutoOrder);
                    var hasAutoOrder = autoOrderItems.Count() > 0;

                    if (hasAutoOrder)
                    {
                        var autoOrderRequest = new CreateAutoOrderRequest(AutoOrderConfiguration, Exigo.GetAutoOrderPaymentType(PropertyBag.PaymentMethod), PropertyBag.AutoOrderStartDate, PropertyBag.ShipMethodID, autoOrderItems, PropertyBag.ShippingAddress)
                        {
                            CustomerID = Identity.Current.CustomerID,
                            Frequency = PropertyBag.AutoOrderFrequencyType
                        };
                        details.Add(autoOrderRequest);
                    }


                    // Create the payment request
                    if (PropertyBag.PaymentMethod is CreditCard)
                    {
                        var card = PropertyBag.PaymentMethod as CreditCard;
                        if (card.Type == CreditCardType.New)
                        {
                            if (hasAutoOrder)
                            {
                                card = Exigo.SaveNewCustomerCreditCard(Identity.Current.CustomerID, card);
                                ((CreateAutoOrderRequest)details.Where(c => c is CreateAutoOrderRequest).FirstOrDefault()).PaymentType = Exigo.GetAutoOrderPaymentType(card);
                            }
                            if (hasOrder)
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
                        }
                        else
                        {
                            if (hasOrder && !Request.IsLocal)
                            {
                                details.Add(new ChargeCreditCardTokenOnFileRequest(card));
                            }
                        }
                    }
                    if (PropertyBag.PaymentMethod is BankAccount)
                    {
                        var account = PropertyBag.PaymentMethod as BankAccount;
                        if (account.Type == ExigoService.BankAccountType.New)
                        {
                            if (hasAutoOrder)
                            {
                                account = Exigo.SaveNewCustomerBankAccount(Identity.Current.CustomerID, account);
                                ((CreateAutoOrderRequest)details.Where(c => c is CreateAutoOrderRequest).FirstOrDefault()).PaymentType = Exigo.GetAutoOrderPaymentType(account);
                            }
                            if (hasOrder)
                            {
                                details.Add(new DebitBankAccountRequest(account));
                            }
                        }
                        else
                        {
                            if (hasOrder)
                            {
                                details.Add(new DebitBankAccountOnFileRequest(account));
                            }
                        }
                    }


                    // Process the transaction
                    var transactionRequest = new TransactionalRequest();
                    transactionRequest.TransactionRequests = details.ToArray();
                    var transactionResponse = Exigo.WebService().ProcessTransaction(transactionRequest);


                    var newOrderID = 0;
                    var newAutoOrderID = 0;
                    if (transactionResponse.Result.Status == ResultStatus.Success)
                    {
                        foreach (var response in transactionResponse.TransactionResponses)
                        {
                            if (response is CreateOrderResponse) newOrderID = ((CreateOrderResponse)response).OrderID;
                            if (response is CreateAutoOrderResponse) newAutoOrderID = ((CreateAutoOrderResponse)response).AutoOrderID;
                        }
                    }

                    PropertyBag.NewOrderID = newOrderID;
                    PropertyBag.NewAutoOrderID = newAutoOrderID;
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
                if (PropertyBag.NewOrderID > 0 || PropertyBag.NewAutoOrderID > 0)
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
                        message = Resources.Common.OrderSubmittingMessage
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
            if (PropertyBag.NewOrderID > 0 || PropertyBag.NewAutoOrderID > 0)
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

        public ActionResult RemoveItemFromCart(Guid id)
        {
            var item = ShoppingCart.Items.Where(c => c.ID == id).FirstOrDefault();
            var subtotal = 0M;
            var itemType = item.Type;

            ShoppingCart.Items.Remove(id);
            Exigo.PropertyBags.Update(ShoppingCart);

            if (ShoppingCart.Items.Where(i => i.Type == itemType).Count() > 0)
            {
                var itemCodes = ShoppingCart.Items.Where(i => i.Type == itemType).Select(c => c.ItemCode);
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
            var cartType = item.Type;

            ShoppingCart.Items.Update(id, quantity);
            Exigo.PropertyBags.Update(ShoppingCart);

            var subtotal = 0M;

            if (quantity == 0)
            {
                item.Quantity = 0;
            }

            if (ShoppingCart.Items.Count() > 0)
            {
                var itemCodes = ShoppingCart.Items.Where(c => c.Type == cartType).Select(c => c.ItemCode);
                var items = Exigo.GetItems(new ExigoService.GetItemsRequest
                {
                    Configuration = OrderConfiguration,
                    ItemCodes = itemCodes.ToArray(),
                    PriceTypeID = (cartType == ShoppingCartItemType.AutoOrder) ? AutoOrderConfiguration.PriceTypeID : OrderConfiguration.PriceTypeID
                }).ToList();

                foreach (var cartItem in items)
                {
                    var shoppingCartItem = ShoppingCart.Items.Where(c => c.ItemCode == cartItem.ItemCode && c.Type == cartType).FirstOrDefault();
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

            // If Auto Order, update the Auto Order ship method id too
            if (ShoppingCart.Items.Any(i => i.Type == ShoppingCartItemType.AutoOrder))
            {
                PropertyBag.AutoOrderShipMethodID = shipMethodID;
            }

            Exigo.PropertyBags.Update(PropertyBag);

            return RedirectToAction("Review");
        }

        [HttpPost]
        public JsonNetResult CalculateOrder(ShippingAddress shippingAddress, List<ShoppingCartItem> items, int shipMethodID = 0)
        {
            var configuration = OrderConfiguration;

            var response = Exigo.CalculateOrder(new OrderCalculationRequest
            {
                Address = shippingAddress,
                Configuration = configuration,
                Items = items,
                ShipMethodID = shipMethodID,
                ReturnShipMethods = true
            });

            return new JsonNetResult(response);
        }
        #endregion
    }
}