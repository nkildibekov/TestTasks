﻿using Common.Api.ExigoWebService;
using ExigoService;
using System.Collections.Generic;
using System.Configuration;

namespace Common
{
    public class UnitedStatesConfiguration : IMarketConfiguration
    {
        private MarketName marketName = MarketName.UnitedStates;

        public MarketName MarketName
        {
            get
            {
                return marketName;
            }
        }

        #region Properties
        // Shopping
        public IOrderConfiguration Orders
        {
            get
            {
                return new OrderConfiguration();
            }
        }
        public IOrderConfiguration AutoOrders
        {
            get
            {
                return new AutoOrderConfiguration();
            }
        }

        // Back Office
        public IOrderConfiguration BackOfficeOrders
        {
            get
            {
                return new BackOfficeOrderConfiguration();
            }
        }
        public IOrderConfiguration BackOfficeAutoOrders
        {
            get
            {
                return new BackOfficeAutoOrderConfiguration();
            }
        }

        // Enrollment Packs
        public IOrderConfiguration EnrollmentKits
        {
            get
            {
                return new EnrollmentKitConfiguration();
            }
        }
        #endregion

        // Base Order Configuration
        public class BaseOrderConfiguration : IOrderConfiguration
        {
            public BaseOrderConfiguration()
            {
                WarehouseID = Warehouses.Default;
                CurrencyCode = CurrencyCodes.DollarsUS;
                PriceTypeID = PriceTypes.Retail;
                LanguageID = Languages.English;
                DefaultCountryCode = "US";
                DefaultShipMethodID = 6;
                AvailableShipMethods = new List<int> { 6, 7 };                                
            }

            
            public int WarehouseID { get; set; }
            public string CurrencyCode { get; set; }
            public int PriceTypeID { get; set; }
            public int LanguageID { get; set; }
            public string DefaultCountryCode { get; set; }
            public int DefaultShipMethodID { get; set; }
            public List<int> AvailableShipMethods { get; set; }
            public int CategoryID { get; set; }
            public int FeaturedCategoryID { get; set; }
        }     

        #region Configurations
        // Replicated Site - Product List
        public class OrderConfiguration : BaseOrderConfiguration
        {
            public OrderConfiguration()
            {
                CategoryID = 32;
            }
        }

        // Replicated Site - Auto Order Manager
        public class AutoOrderConfiguration : BaseOrderConfiguration
        {
            public AutoOrderConfiguration()
            {
                CategoryID = 33;
            }
        }



        // Replicated Site - Enrollment Kits
        public class EnrollmentKitConfiguration : BaseOrderConfiguration
        {
            public EnrollmentKitConfiguration()
            {
                CategoryID = 101;
            }
        }



        // Back Office - Product List
        public class BackOfficeOrderConfiguration : BaseOrderConfiguration
        {
            public BackOfficeOrderConfiguration()
            {
                CategoryID = 32;
                PriceTypeID = PriceTypes.Wholesale;
            }
        }

        // Back Office - Auto Order Manager
        public class BackOfficeAutoOrderConfiguration : BaseOrderConfiguration
        {
            public BackOfficeAutoOrderConfiguration()
            {
                CategoryID = 33;
                PriceTypeID = PriceTypes.Wholesale;
            }
        }
        #endregion
    }
}