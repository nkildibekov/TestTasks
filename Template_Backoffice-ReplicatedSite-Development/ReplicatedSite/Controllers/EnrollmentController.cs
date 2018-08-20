﻿using Common;
using Common.Api.ExigoWebService;
using Common.Providers;
using Common.HtmlHelpers;
using Common.Services;
using ExigoService;
using ReplicatedSite.Factories;
using ReplicatedSite.Models;
using ReplicatedSite.Providers;
using ReplicatedSite.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Dapper;

namespace ReplicatedSite.Controllers
{
    [RoutePrefix("{webalias}/enrollment")]
    public class EnrollmentController : Controller
    {
        #region Constructor
        public EnrollmentController()
        {
            var market = (PropertyBag.EnrollerID != 0) ? PropertyBag.SelectedMarket : GlobalSettings.Markets.AvailableMarkets.FirstOrDefault().Name;
            this.OrderConfiguration = Common.GlobalUtilities.GetMarketConfiguration(market).Orders;
            this.AutoOrderConfiguration = Common.GlobalUtilities.GetMarketConfiguration(market).AutoOrders;
            this.OrderPacksConfiguration = Common.GlobalUtilities.GetMarketConfiguration(market).EnrollmentKits;
        }
        #endregion

        #region Properties
        public string ApplicationName = Common.GlobalSettings.Globalization.CookieKey + "Enrollment";

        public IOrderConfiguration OrderConfiguration { get; set; }
        public IOrderConfiguration AutoOrderConfiguration { get; set; }
        public IOrderConfiguration OrderPacksConfiguration { get; set; }

        public ShoppingCartItemsPropertyBag ShoppingCart
        {
            get
            {
                if (_shoppingCart == null)
                {
                    _shoppingCart = Exigo.PropertyBags.Get<ShoppingCartItemsPropertyBag>(ApplicationName + "Cart");
                }
                return _shoppingCart;
            }
        }
        private ShoppingCartItemsPropertyBag _shoppingCart;

        public EnrollmentPropertyBag PropertyBag
        {
            get
            {
                if (_propertyBag == null)
                {
                    _propertyBag = Exigo.PropertyBags.Get<EnrollmentPropertyBag>(ApplicationName + "PropertyBag");
                }
                return _propertyBag;
            }
        }
        private EnrollmentPropertyBag _propertyBag;

        public ILogicProvider LogicProvider
        {
            get
            {
                if (_logicProvider == null)
                {
                    _logicProvider = new EnrollmentLogicProvider(this, ShoppingCart, PropertyBag);
                }
                return _logicProvider;
            }
        }
        private ILogicProvider _logicProvider;
        #endregion

        #region Views

        //[Route("products/ChooseEnroller")]
        public ActionResult ChooseEnroller()
        {
            return View();
        }

        public ActionResult Index()
        {
            _propertyBag = Exigo.PropertyBags.Delete(PropertyBag);
            _shoppingCart = Exigo.PropertyBags.Delete(ShoppingCart);

            return View();
        }

        public ActionResult EnrollmentConfiguration(EnrollmentConfigurationViewModel enroller = null)
        {
            if (Request.HttpMethod == "GET")
            {
                var model = new EnrollmentConfigurationViewModel();
                model.MarketSelectList = GlobalSettings.Markets.AvailableMarkets.Select(c => new SelectListItem
                {
                    Text = c.Description,
                    Value = c.Name.ToString(),
                    Selected = (c.CookieValue == "CA")
                }).ToList();
                model.MarketName = Utilities.GetCurrentMarket().Name;

                model.EnrollerID = Identity.Owner.CustomerID;

                return View(model);
            }
            else
            {
                // set the customer's addresses to the country selected
                var countryCode = GlobalSettings.Markets.AvailableMarkets.Where(c => c.Name == enroller.MarketName).FirstOrDefault().CookieValue;
                PropertyBag.Customer.MainAddress.Country = countryCode;
                PropertyBag.Customer.MailingAddress.Country = countryCode;
                PropertyBag.ShippingAddress.Country = countryCode;
                PropertyBag.SelectedMarket = enroller.MarketName;
                PropertyBag.EnrollerID = enroller.EnrollerID;

                // Set the enroller information
                var enrollerInfo = new Customer();
                if (PropertyBag.EnrollerID == Identity.Owner.CustomerID)
                {
                    enrollerInfo.CustomerID = Identity.Owner.CustomerID;
                    enrollerInfo.FirstName = Identity.Owner.FirstName;
                    enrollerInfo.LastName = Identity.Owner.LastName;
                    enrollerInfo.Email = Identity.Owner.Email;
                    enrollerInfo.PrimaryPhone = Identity.Owner.Phone;
                }
                else
                {
                    enrollerInfo = Exigo.GetCustomer(enroller.EnrollerID);
                }

                PropertyBag.Customer.Enroller = enrollerInfo;

                //Exigo.GetCustomer(enroller.EnrollerID);
                Exigo.PropertyBags.Update(PropertyBag);
                return RedirectToAction("Checkout");
            }
        }

        public ActionResult Packs()
        {
            var model = new PacksViewModel();
            if (ShoppingCart.Items.Where(c => c.Type == ShoppingCartItemType.EnrollmentPack).Count() > 0)
            {
                model.SelectedOrderItem = ShoppingCart.Items.Where(c => c.Type == ShoppingCartItemType.EnrollmentPack).FirstOrDefault();
            }

            model.CustomerTypeID = (PropertyBag.Customer != null) ? PropertyBag.Customer.CustomerTypeID : 1;

            model.OrderItems = Exigo.GetItems(new ExigoService.GetItemsRequest
            {
                Configuration = OrderPacksConfiguration,
                IncludeChildCategories = true
            }).ToList();

            foreach (var item in model.OrderItems)
            {
                item.Type = ShoppingCartItemType.EnrollmentPack;
            }


            return View(model);
        }

        public ActionResult ProductList()
        {
            var model = new EnrollmentProductListViewModel();

            model.OrderItems = Exigo.GetItems(new ExigoService.GetItemsRequest
            {
                Configuration = OrderConfiguration
            }).ToList();

            var autoOrderItems = Exigo.GetItems(new ExigoService.GetItemsRequest
            {
                Configuration = AutoOrderConfiguration
            }).ToList();
            autoOrderItems.ForEach(c => c.Type = ShoppingCartItemType.AutoOrder);
            model.AutoOrderItems = autoOrderItems;

            return View(model);
        }

        public ActionResult EnrolleeInfo(FormCollection form = null)
        {
            if (Request.HttpMethod == "GET")
            {
                if (PropertyBag.Customer.Enroller == null)
                {
                    return Checkout();
                }

                return View(PropertyBag);
            }
            else
            {
                // Fixes issues with payment methods not being saved correctly
                PropertyBag.PaymentMethod = null;

                var type = typeof(EnrollmentPropertyBag);
                var binder = Binders.GetBinder(type);
                var bindingContext = new ModelBindingContext()
                {
                    ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => PropertyBag, type),
                    ModelState = ModelState,
                    ValueProvider = form,

                };
                binder.BindModel(ControllerContext, bindingContext);

                // Set shipping address
                if (PropertyBag.UseSameShippingAddress)
                {
                    PropertyBag.ShippingAddress = new ShippingAddress(PropertyBag.Customer.MainAddress);
                    PropertyBag.ShippingAddress.FirstName = PropertyBag.Customer.FirstName;
                    PropertyBag.ShippingAddress.LastName = PropertyBag.Customer.LastName;
                    PropertyBag.ShippingAddress.Phone = PropertyBag.Customer.PrimaryPhone;
                    PropertyBag.ShippingAddress.Email = PropertyBag.Customer.Email;
                }

                Exigo.PropertyBags.Update(PropertyBag);

                return new JsonNetResult(new
                {
                    success = true
                });
            }

        }

        public ActionResult SetEnroller()
        {
            if (PropertyBag.Customer.Enroller == null && Identity.Owner.CustomerID != GlobalSettings.ReplicatedSites.DefaultAccountID)
            {
                var enrollerInfo = Exigo.GetCustomer(Identity.Owner.CustomerID);
                PropertyBag.Customer.Enroller = enrollerInfo;
                Exigo.PropertyBags.Update(PropertyBag);
            }
            else if (Identity.Owner.CustomerID == GlobalSettings.ReplicatedSites.DefaultAccountID)
            {
                return EnrollmentConfiguration();
            }
            return Checkout();
        }

        public ActionResult Review()
        {
            var model = EnrollmentViewModelFactory.Create<EnrollmentReviewViewModel>(PropertyBag);
            var languageID = Exigo.GetSelectedLanguageID();

            // Get the cart items
            var cartItems = ShoppingCart.Items.Where(c => c.Type == ShoppingCartItemType.Order || c.Type == ShoppingCartItemType.EnrollmentPack).ToList();
            model.Items = Exigo.GetItems(ShoppingCart.Items, OrderConfiguration, languageID).ToList();

            var calculationResult = Exigo.CalculateOrder(new OrderCalculationRequest
            {
                Configuration = OrderConfiguration,
                Items = cartItems,
                Address = PropertyBag.ShippingAddress,
                ShipMethodID = PropertyBag.ShipMethodID,
                ReturnShipMethods = true
            });

            model.Totals = calculationResult;
            model.ShipMethods = calculationResult.ShipMethods;


            // Set the default ship method
            var shipMethodID = 0;

            if (PropertyBag.ShipMethodID != 0)
            {
                shipMethodID = PropertyBag.ShipMethodID;
            }
            else
            {
                shipMethodID = OrderConfiguration.DefaultShipMethodID;
                PropertyBag.ShipMethodID = OrderConfiguration.DefaultShipMethodID;
                Exigo.PropertyBags.Update(PropertyBag);
            }

            if (model.ShipMethods != null)
            {
                if (model.ShipMethods.Any(c => c.ShipMethodID == shipMethodID))
                {
                    model.ShipMethods.First(c => c.ShipMethodID == shipMethodID).Selected = true;
                }
                else
                {
                    // If we don't have the ship method we're supposed to select, 
                    // check the first one, save the selection and recalculate
                    model.ShipMethods.First().Selected = true;

                    PropertyBag.ShipMethodID = model.ShipMethods.First().ShipMethodID;
                    Exigo.PropertyBags.Update(PropertyBag);

                    var newCalculationResult = Exigo.CalculateOrder(new OrderCalculationRequest
                    {
                        Configuration = OrderConfiguration,
                        Items = cartItems,
                        Address = PropertyBag.ShippingAddress,
                        ShipMethodID = PropertyBag.ShipMethodID,
                        ReturnShipMethods = false
                    });

                    model.Totals = newCalculationResult;
                }
            }
            else
            {

            }


            return View(model);
        }

        public ActionResult EnrollmentComplete(string token)
        {
            var model = new EnrollmentCompleteViewModel();
            var args = Security.Decrypt(token);
            var hasOrder = args["OrderID"] != null;
            var hasAutoOrder = args["AutoOrderID"] != null;

            model.CustomerID = Convert.ToInt32(args["CustomerID"]);
            if (hasOrder)
            {
                model.OrderID = Convert.ToInt32(args["OrderID"]);
                model.Order = Exigo.GetCustomerOrders(new GetCustomerOrdersRequest
                {
                    CustomerID = model.CustomerID,
                    OrderID = model.OrderID,
                    IncludeOrderDetails = true,
                    IncludePayments = true
                }).FirstOrDefault();
            }
            if (hasAutoOrder)
            {
                model.AutoOrderID = Convert.ToInt32(args["AutoOrderID"]);
                try
                {
                    model.AutoOrder = Exigo.GetCustomerAutoOrders(Identity.Customer.CustomerID, model.AutoOrderID).FirstOrDefault();
                }
                catch { }
            }

            return View(model);
        }

        public ActionResult Checkout()
        {
            return LogicProvider.GetNextAction();
        }
        #endregion

        #region Ajax
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
                    var apiRequests = new List<ApiRequest>();
                                     
                    // Create the customer
                    var customerRequest                = new CreateCustomerRequest(PropertyBag.Customer);
                    customerRequest.InsertEnrollerTree = true;
                    customerRequest.EnrollerID         = PropertyBag.EnrollerID;
                    customerRequest.CustomerType       = CustomerTypes.Distributor;
                    customerRequest.EntryDate          = DateTime.Now.ToCST();
                    customerRequest.CustomerStatus     = CustomerStatuses.Active;
                    customerRequest.CanLogin           = true;
                    customerRequest.Notes              = "Distributor was entered by Distributor #{0}. Created by the API Enrollment at ".FormatWith(Identity.Owner.CustomerID) + HttpContext.Request.Url.Host + HttpContext.Request.Url.LocalPath + " on " + DateTime.Now.ToCST().ToString("dddd, MMMM d, yyyy h:mmtt") + " CST at IP " + Common.GlobalUtilities.GetClientIP() + " using " + HttpContext.Request.Browser.Browser + " " + HttpContext.Request.Browser.Version + " (" + HttpContext.Request.Browser.Platform + ").";
                    apiRequests.Add(customerRequest);


                    // Set a few variables up for our shippping address, order/auto order items and the default auto order payment type
                    var shippingAddress = PropertyBag.ShippingAddress;
                    var orderItems = ShoppingCart.Items.Where(i => i.Type == ShoppingCartItemType.Order || i.Type == ShoppingCartItemType.EnrollmentPack).ToList();
                    var autoOrderItems = ShoppingCart.Items.Where(i => i.Type == ShoppingCartItemType.AutoOrder || i.Type == ShoppingCartItemType.EnrollmentAutoOrderPack).ToList();
                    var autoOrderPaymentType = AutoOrderPaymentType.PrimaryCreditCard;

                    // Create initial order
                    var orderRequest = new CreateOrderRequest(OrderConfiguration, PropertyBag.ShipMethodID, orderItems, shippingAddress);

                    // Add the new credit card to the customer's record and charge it for the current order
                    if (PropertyBag.PaymentMethod.CanBeParsedAs<CreditCard>())
                    {
                        var creditCard = PropertyBag.PaymentMethod.As<CreditCard>();

                        if (!creditCard.IsTestCreditCard && !Request.IsLocal)
                        {
                            var chargeCCRequest = new ChargeCreditCardTokenRequest(creditCard);
                            apiRequests.Add(chargeCCRequest);

                            var saveCCRequest = new SetAccountCreditCardTokenRequest(creditCard);
                            apiRequests.Add(saveCCRequest);
                        }
                        else
                        {
                            orderRequest.OrderStatus = GlobalUtilities.GetDefaultOrderStatusType();
                        }
                    }

                    // Add order request now if we need to do any testing with the accepted functionality
                    apiRequests.Add(orderRequest);

                    if (PropertyBag.PaymentMethod.CanBeParsedAs<BankAccount>())
                    {
                        var bankAccount = PropertyBag.PaymentMethod.As<BankAccount>();
                        if (bankAccount.Type == ExigoService.BankAccountType.New)
                        {
                            apiRequests.Add(new DebitBankAccountRequest(bankAccount));
                        }
                        else
                        {
                            apiRequests.Add(new DebitBankAccountOnFileRequest(bankAccount));
                        }
                    }

                    // Create subscription autoorder if an autoorder has been chosen
                    if (autoOrderItems != null && autoOrderItems.Count() > 0)
                    {
                        var autoOrderRequest = new CreateAutoOrderRequest(AutoOrderConfiguration, autoOrderPaymentType, DateTime.Now.AddMonths(1).ToCST(), PropertyBag.ShipMethodID, autoOrderItems, shippingAddress);
                        apiRequests.Add(autoOrderRequest);
                    }

                    // Create customer site
                    var customerSiteRequest = new SetCustomerSiteRequest(PropertyBag.Customer);
                    apiRequests.Add(customerSiteRequest);


                    // Process the transaction
                    var transaction = new TransactionalRequest { TransactionRequests = apiRequests.ToArray() };
                    var response = Exigo.WebService().ProcessTransaction(transaction);

                    var newcustomerid = 0;
                    var neworderid = 0;
                    var newautoorderid = 0;

                    if (response.Result.Status == ResultStatus.Success)
                    {
                        foreach (var apiresponse in response.TransactionResponses)
                        {
                            if (apiresponse.CanBeParsedAs<CreateCustomerResponse>()) newcustomerid = apiresponse.As<CreateCustomerResponse>().CustomerID;
                            if (apiresponse.CanBeParsedAs<CreateOrderResponse>()) neworderid = apiresponse.As<CreateOrderResponse>().OrderID;
                            if (apiresponse.CanBeParsedAs<CreateAutoOrderResponse>()) newautoorderid = apiresponse.As<CreateAutoOrderResponse>().AutoOrderID;
                        }
                    }

                    PropertyBag.NewCustomerID = newcustomerid;
                    PropertyBag.NewOrderID = neworderid;
                    PropertyBag.NewAutoOrderID = newautoorderid;
                    _propertyBag = Exigo.PropertyBags.Update(PropertyBag);

                    // If the transaction was successful, then send the customer an email that will allow them to confirm thier opt in choice 
                    if (PropertyBag.Customer.IsOptedIn)
                    {
                        Exigo.SendEmailVerification(newcustomerid, PropertyBag.Customer.Email);
                    }

                    var token = Security.Encrypt(new
                    {
                        CustomerID = newcustomerid,
                        OrderID = neworderid,
                        AutoOrderID = newautoorderid
                    });

                    // Enrollment complete, now delete the Property Bag
                    Exigo.PropertyBags.Delete(PropertyBag);
                    Exigo.PropertyBags.Delete(ShoppingCart);

                    return new JsonNetResult(new
                    {
                        success = true,
                        token = token
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
                if (PropertyBag.NewCustomerID > 0)
                {
                    var token = Security.Encrypt(new
                    {
                        CustomerID = PropertyBag.NewCustomerID,
                        OrderID = PropertyBag.NewOrderID,
                        AutoOrderID = PropertyBag.NewAutoOrderID
                    });

                    return new JsonNetResult(new
                    {
                        success = true,
                        token = token
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

        [AllowAnonymous]
        [HttpPost]
        public JsonNetResult GetDistributors(string query)
        {
            try
            {
                // assemble a list of customers who match the search criteria
                var enrollerCollection = new List<SearchResult>();
                var isCustomerID = query.CanBeParsedAs<int>();

                var nodeDataRecords = new List<dynamic>();
                if (isCustomerID)
                {
                    using (var context = Exigo.Sql())
                    {
                        nodeDataRecords = context.Query(@"
                                SELECT
                                    cs.CustomerID, cs.FirstName, cs.LastName, cs.WebAlias,
                                    c.MainCity, c.MainState, c.MainCountry
                                FROM CustomerSites cs
                                INNER JOIN Customers c
                                ON cs.CustomerID = c.CustomerID
                                WHERE c.CustomerTypeID = @customertypeid
                                AND cs.CustomerID = @customerid
                        ", new
                        {
                            customertypeid = CustomerTypes.Distributor,
                            customerid = query
                        }).ToList();
                    }
                }
                else
                {
                    using (var context = Exigo.Sql())
                    {
                        nodeDataRecords = context.Query(@"
                                SELECT
                                    cs.CustomerID, cs.FirstName, cs.LastName, cs.WebAlias,
                                    c.MainCity, c.MainState, c.MainCountry
                                FROM CustomerSites cs
                                INNER JOIN Customers c
                                ON cs.CustomerID = c.CustomerID
                                WHERE c.CustomerTypeID = @customertypeid
                                AND (c.FirstName LIKE @queryValue OR c.LastName LIKE @queryvalue OR cs.FirstName LIKE @queryValue OR cs.LastName LIKE @queryValue)
                        ", new
                        {
                            customertypeid = CustomerTypes.Distributor,
                            queryValue = "%" + query + "%"
                        }).ToList();
                    }
                }

                if (nodeDataRecords.Count() > 0)
                {
                    foreach (var record in nodeDataRecords)
                    {
                        var node = new SearchResult();
                        node.CustomerID = record.CustomerID;
                        node.FirstName = record.FirstName;
                        node.LastName = record.LastName;
                        node.MainCity = record.MainCity;
                        node.MainState = record.MainState;
                        node.MainCountry = record.MainCountry;
                        node.WebAlias = record.WebAlias;
                        enrollerCollection.Add(node);
                    }
                }


                var urlHelper = new UrlHelper(Request.RequestContext);
                foreach (var item in enrollerCollection)
                {
                    item.AvatarURL = urlHelper.Avatar(item.CustomerID);
                }

                return new JsonNetResult(new
                {
                    success = true,
                    enrollers = enrollerCollection
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
            if (PropertyBag.NewCustomerID > 0)
            {
                var token = Security.Encrypt(new
                {
                    CustomerID = PropertyBag.NewCustomerID,
                    OrderID = PropertyBag.NewOrderID,
                    AutoOrderID = PropertyBag.NewAutoOrderID
                });

                return new JsonNetResult(new
                {
                    success = true,
                    token = token
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
        public JsonNetResult UpdatePackItems(string itemcode, string action, int packType)
        {
            try
            {
                if (packType == (int)ShoppingCartItemType.EnrollmentPack)
                {
                    ShoppingCart.Items.Remove(ShoppingCartItemType.EnrollmentPack);

                    if (action == "add")
                    {
                        ShoppingCart.Items.Add(new ShoppingCartItem()
                        {
                            ItemCode = itemcode,
                            Quantity = 1,
                            Type = ShoppingCartItemType.EnrollmentPack
                        });
                    }

                    Exigo.PropertyBags.Update(ShoppingCart);

                    return new JsonNetResult(new
                    {
                        success = true
                    });
                }
                else if (packType == (int)ShoppingCartItemType.EnrollmentAutoOrderPack)
                {
                    ShoppingCart.Items.Remove(ShoppingCartItemType.EnrollmentAutoOrderPack);

                    if (action == "add")
                    {
                        ShoppingCart.Items.Add(new ShoppingCartItem()
                        {
                            ItemCode = itemcode,
                            Quantity = 1,
                            Type = ShoppingCartItemType.EnrollmentAutoOrderPack
                        });
                    }

                    Exigo.PropertyBags.Update(ShoppingCart);

                    return new JsonNetResult(new
                    {
                        success = true
                    });
                }
                else
                {
                    return new JsonNetResult(new
                    {
                        success = false
                    });
                }

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

        public JsonNetResult UpdateItemSummary()
        {
            try
            {
                var model = new EnrollmentSummaryViewModel();
                var order = new OrderCalculationResponse();
                var hasShippingAddress = (PropertyBag.ShippingAddress != null && PropertyBag.ShippingAddress.IsComplete);
                var orderItems = ShoppingCart.Items.Where(c => c.Type == ShoppingCartItemType.Order || c.Type == ShoppingCartItemType.EnrollmentPack).ToList();
                var languageID = Exigo.GetSelectedLanguageID();

                if (hasShippingAddress)
                {
                    model.IsCalculated = true;
                    order = Exigo.CalculateOrder(new OrderCalculationRequest
                    {
                        Configuration = OrderConfiguration,
                        Items = orderItems,
                        Address = PropertyBag.ShippingAddress,
                        ShipMethodID = PropertyBag.ShipMethodID,
                        ReturnShipMethods = true
                    });

                    model.Total = order.Total;
                    model.Shipping = order.Shipping;
                    model.Tax = order.Tax;
                }

                model.OrderItems = Exigo.GetItems(ShoppingCart.Items.Where(c => c.Type == ShoppingCartItemType.Order), OrderConfiguration, languageID);
                model.AutoOrderItems = Exigo.GetItems(ShoppingCart.Items.Where(c => c.Type == ShoppingCartItemType.AutoOrder), AutoOrderConfiguration, languageID);
                model.OrderEnrollmentPacks = Exigo.GetItems(ShoppingCart.Items.Where(c => c.Type == ShoppingCartItemType.EnrollmentPack), OrderPacksConfiguration, languageID);
                model.AutoOrderEnrollmentPacks = Exigo.GetItems(ShoppingCart.Items.Where(c => c.Type == ShoppingCartItemType.EnrollmentAutoOrderPack), AutoOrderConfiguration, languageID);

                var autoOrderSubtotal = model.AutoOrderItems.Sum(c => c.Price * c.Quantity);
                var autoOrderPackSubtotal = model.AutoOrderEnrollmentPacks.Sum(c => c.Price * c.Quantity);
                var orderSubtotal = model.OrderItems.Sum(c => c.Price * c.Quantity);
                var orderPackSubtotal = model.OrderEnrollmentPacks.Sum(c => c.Price * c.Quantity);
                model.OrderSubtotal = (hasShippingAddress) ? order.Subtotal : (orderSubtotal + orderPackSubtotal);
                model.AutoOrderSubtotal = autoOrderPackSubtotal + autoOrderSubtotal;

                return new JsonNetResult(new
                {
                    success = true,
                    orderitems = ShoppingCart.Items.Where(c => c.Type == ShoppingCartItemType.Order),
                    autoorderitems = ShoppingCart.Items.Where(c => c.Type == ShoppingCartItemType.AutoOrder),
                    html = this.RenderPartialViewToString("_EnrollmentSummary", model)
                });
            }
            catch (Exception ex)
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        public JsonNetResult AddItemToCart(Item item)
        {
            try
            {
                ShoppingCart.Items.Add(item);
                Exigo.PropertyBags.Update(ShoppingCart);

                return new JsonNetResult(new
                {
                    success = true
                });
            }
            catch (Exception)
            {

                return new JsonNetResult(new
                {
                    success = false
                });
            }

        }

        public ActionResult UpdateItemQuantity(string itemcode, string type, decimal quantity)
        {
            try
            {
                var itemType = ConvertItemType(type);
                var item = ShoppingCart.Items.Where(c => c.ItemCode == itemcode && c.Type == itemType).FirstOrDefault();

                ShoppingCart.Items.Update(item.ID, quantity);
                Exigo.PropertyBags.Update(ShoppingCart);

                return new JsonNetResult(new
                {
                    success = true
                });
            }
            catch (Exception)
            {

                return new JsonNetResult(new
                {
                    success = false
                });
            }
        }

        public JsonNetResult DeleteItemFromCart(string itemcode, string type)
        {
            try
            {
                var itemType = ConvertItemType(type);
                var itemID = ShoppingCart.Items.Where(c => c.ItemCode == itemcode && c.Type == itemType).FirstOrDefault().ID;

                ShoppingCart.Items.Remove(itemID);
                Exigo.PropertyBags.Update(ShoppingCart);

                return new JsonNetResult(new
                {
                    success = true
                });
            }
            catch (Exception)
            {

                return new JsonNetResult(new
                {
                    success = false
                });
            }
        }

        [HttpPost]
        public ActionResult SetShipMethodID(int shipMethodID)
        {
            PropertyBag.ShipMethodID = shipMethodID;
            Exigo.PropertyBags.Update(PropertyBag);

            return RedirectToAction("Review");
        }

        [OutputCache(Duration = 600, VaryByParam = "itemcategoryid")]
        public JsonNetResult GetItems(int itemcategoryid)
        {
            var items = Exigo.GetItems(new ExigoService.GetItemsRequest
            {
                Configuration = OrderPacksConfiguration,
                CategoryID = itemcategoryid
            }).ToList();



            var html = this.RenderPartialViewToString("_ProductList", items);


            return new JsonNetResult(new
            {
                success = true,
                items = items,
                html = html
            });
        }
        #endregion

        #region Helpers
        public ShoppingCartItemType ConvertItemType(string type)
        {
            var itemType = new ShoppingCartItemType();
            switch (type)
            {
                case "Order":
                    itemType = ShoppingCartItemType.Order;
                    break;
                case "AutoOrder":
                    itemType = ShoppingCartItemType.AutoOrder;
                    break;
                case "EnrollmentPack":
                    itemType = ShoppingCartItemType.EnrollmentPack;
                    break;
                case "EnrollmentAutoOrderPack":
                    itemType = ShoppingCartItemType.EnrollmentAutoOrderPack;
                    break;
                default:
                    break;
            }
            return itemType;
        }
        #endregion

        #region Models and Enums
        public class SearchResult
        {
            public int CustomerID { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string FullName
            {
                get { return this.FirstName + " " + this.LastName; }
            }
            public string AvatarURL { get; set; }
            public string WebAlias { get; set; }
            public string ReplicatedSiteUrl
            {
                get
                {
                    if (string.IsNullOrEmpty(this.WebAlias)) return "";
                    else return GlobalSettings.ReplicatedSites.FormattedBaseUrl.FormatWith(WebAlias);
                }
            }
            public string MainState { get; set; }
            public string MainCity { get; set; }
            public string MainCountry { get; set; }
        }
        #endregion

        #region Mocked Dynamic Kit Data
        //******** MOCKED DATA FOR DYNAMIC KIT TESTING, DO NOT DELETE **********//
        /*
        // Mock the dynamic kit items
        var masterItem = new Item() { ItemCode = "MASTER", ItemDescription = "Master Item", Price = 29.95M, IsDynamicKitMaster = true };

        // Create some mock dynamic kit categories
        var mockedDynamicKitCategories = new List<DynamicKitCategory>();
        var dynamicKitCategories = new List<string>() { "A" };

        // Mock Settings
        var randomize = false;
        var random = new Random();
        int categorycounter = 0;

        foreach (var dynamicKitCategory in dynamicKitCategories)
        {
            // Determine how many items will be in this category
            var categoryItemsCount = (randomize) ? random.Next(3, 10) : 5;


            // Create the category
            var category = new DynamicKitCategory()
            {
                DynamicKitCategoryID = categorycounter,
                DynamicKitCategoryDescription = "Category " + dynamicKitCategory,
                Quantity = (randomize) ? random.Next(5, 20) : 5
            };


            // Add some items
            var categoryItems = new List<DynamicKitCategoryItem>();
            for (int i = 1; i < categoryItemsCount + 1; i++)
            {
                categoryItems.Add(new DynamicKitCategoryItem()
                {
                    ItemID = i + 1000,
                    ItemCode = "KITITEM-" + dynamicKitCategory + "-" + i,
                    ItemDescription = "Item " + i + " - Category " + dynamicKitCategory
                });
            }
            category.Items = categoryItems;


            // Add our new category to our master item code
            mockedDynamicKitCategories.Add(category);


            // Increment the counter
            categorycounter++;
        }

        // Set our master item's categories
        masterItem.DynamicKitCategories = mockedDynamicKitCategories;

        // Add our finished mocked dynamic kit item to our collection
        model.Add(masterItem);
        */
        #endregion
    }
}

