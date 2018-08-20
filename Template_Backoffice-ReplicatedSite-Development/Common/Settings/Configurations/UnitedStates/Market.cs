using ExigoService;
using System.Collections.Generic;

namespace Common
{
    public class UnitedStatesMarket : Market
    {
        public UnitedStatesMarket()
            : base()
        {
            Name        = MarketName.UnitedStates;
            Description = "United States";
            CookieValue = "US";
            CultureCode = "en-US";
            IsDefault   = true;
            Countries   = new List<string> { "US" };

            AvailablePaymentTypes = new List<IPaymentMethod>
            {
                new CreditCard()
                ,new BankAccount()
            };

            AvailableLanguages = new List<Language> 
            { 
                new Language(Languages.English, "en-US"),
                new Language(Languages.Spanish, "es-US")
            };

            AvailableAutoOrderFrequencyTypes = new List<Common.Api.ExigoWebService.FrequencyType>
            {
                Api.ExigoWebService.FrequencyType.Monthly,
                Api.ExigoWebService.FrequencyType.BiWeekly,              
                Api.ExigoWebService.FrequencyType.Weekly
            };

            AvailableShipMethods = new List<int> { 6, 7 };
        }

        public override IMarketConfiguration GetConfiguration()
        {
            return new UnitedStatesConfiguration();
        }
    }
}