using ExigoService;
using System.Collections.Generic;

namespace Common
{
    public class CanadaMarket : Market
    {
        public CanadaMarket()
            : base()
        {
            Name        = MarketName.Canada;
            Description = "Canada";
            CookieValue = "CA";
            CultureCode = "en-US";
            IsDefault   = true;
            Countries   = new List<string> { "CA" };

            AvailablePaymentTypes = new List<IPaymentMethod>
            {
                new CreditCard()                
            };

            AvailableLanguages = new List<Language> 
            { 
                new Language(Languages.English, "en-US")
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
            return new CanadaConfiguration();
        }
    }
}