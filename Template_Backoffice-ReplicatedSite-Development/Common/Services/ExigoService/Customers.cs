using Common;
using Common.Api.ExigoWebService;
using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace ExigoService
{
    public static partial class Exigo
    {
        public static Customer GetCustomer(int customerID)
        {
            var customer = new Customer();
            using (var context = Exigo.Sql())
            {
                customer = context.Query<Customer, Address, Address, Address, Customer>(@"                                
                                    SELECT c.CustomerID
                                          ,c.FirstName
                                          ,c.MiddleName
                                          ,c.LastName
                                          ,c.NameSuffix
                                          ,c.Company
                                          ,c.CustomerTypeID
                                          ,c.CustomerStatusID
                                          ,c.Email
                                          ,PrimaryPhone = c.Phone
                                          ,SecondaryPhone = c.Phone2
                                          ,c.MobilePhone
                                          ,c.Fax
                                          ,c.CanLogin
                                          ,c.LoginName
                                          ,c.PasswordHash
                                          ,c.RankID
                                          ,c.EnrollerID
                                          ,c.SponsorID
                                          ,c.BirthDate
                                          ,c.CurrencyCode
                                          ,c.PayableToName
                                          ,c.DefaultWarehouseID
                                          ,c.PayableTypeID
                                          ,c.CheckThreshold
                                          ,c.LanguageID
                                          ,c.Gender
                                          ,c.TaxCode
                                          ,c.TaxCodeTypeID
                                          ,c.IsSalesTaxExempt
                                          ,c.SalesTaxCode
                                          ,c.SalesTaxExemptExpireDate
                                          ,c.VatRegistration
                                          ,c.BinaryPlacementTypeID
                                          ,c.UseBinaryHoldingTank
                                          ,c.IsInBinaryHoldingTank
                                          ,IsOptedIn = c.IsEmailSubscribed
                                          ,c.IsSMSSubscribed
                                          ,c.EmailSubscribeIP
                                          ,c.Notes
                                          ,c.Field2
                                          ,c.Field3
                                          ,c.Field4
                                          ,c.Field5
                                          ,c.Field6
                                          ,c.Field7
                                          ,c.Field8
                                          ,c.Field9
                                          ,c.Field10
                                          ,c.Field11
                                          ,c.Field12
                                          ,c.Field13
                                          ,c.Field14
                                          ,c.Field15
                                          ,c.Date1
                                          ,c.Date2
                                          ,c.Date3
                                          ,c.Date4
                                          ,c.Date5
                                          ,c.CreatedDate
                                          ,c.ModifiedDate
                                          ,c.CreatedBy
                                          ,c.ModifiedBy
	                                      ,cs.CustomerStatusDescription
                                          ,ct.CustomerTypeDescription
                                          ,Address1 = c.MainAddress1
                                          ,Address2 = c.MainAddress2
                                          ,Address3 = c.MainAddress3
                                          ,City = c.MainCity
                                          ,State = c.MainState
                                          ,Zip = c.MainZip
                                          ,Country = c.MainCountry
                                          ,County = c.MainCounty
                                          ,Address1 = c.MailAddress1
                                          ,Address2 = c.MailAddress2
                                          ,Address3 = c.MailAddress3
                                          ,City = c.MailCity
                                          ,State = c.MailState
                                          ,Zip = c.MailZip
                                          ,Country = c.MailCountry
                                          ,County = c.MailCounty
                                          ,Address1 = c.OtherAddress1
                                          ,Address2 = c.OtherAddress2
                                          ,Address3 = c.OtherAddress3
                                          ,City = c.OtherCity
                                          ,State = c.OtherState
                                          ,Zip = c.OtherZip
                                          ,Country = c.OtherCountry
                                          ,County = c.OtherCounty
                                FROM Customers c
	                                LEFT JOIN CustomerStatuses cs
		                                ON c.CustomerStatusID = cs.CustomerStatusID
	                                LEFT JOIN CustomerTypes ct
		                                ON c.CustomerTypeID = ct.CustomerTypeID
                                WHERE c.CustomerID = @CustomerID
                    ", (cust, main, mail, other) =>
                {
                    main.AddressType = AddressType.Main;
                    cust.MainAddress = main;
                    mail.AddressType = AddressType.Mailing;
                    cust.MailingAddress = mail;
                    other.AddressType = AddressType.Other;
                    cust.OtherAddress = other;
                    return cust;
                },
                     param: new
                     {
                         CustomerID = customerID
                     }, splitOn: "Address1, Address1, Address1"
                     ).FirstOrDefault();
            }


            if (customer == null) return null;

            return customer;
        }
        public static Customer GetCustomerRealTime(int customerID)
        {
            var customer = Exigo.WebService().GetCustomers(new GetCustomersRequest { CustomerID = customerID }).Customers.FirstOrDefault();
            return (customer != null) ? (Customer)customer : null;
        }
        public static IEnumerable<CustomerWallItem> GetCustomerRecentActivity(GetCustomerRecentActivityRequest request)
        {
            List<CustomerWallItem> wallItems;
            using (var context = Exigo.Sql())
            {
                wallItems = context.Query<CustomerWallItem>(@"
                                SELECT CustomerWallItemID
                                      ,CustomerID
                                      ,EntryDate
                                      ,Text
                                      ,Field1
                                      ,Field2
                                      ,Field3
                                FROM CustomerWall
                                WHERE CustomerID = @CustomerID
                    ", new
                     {
                         CustomerID = request.CustomerID
                     }).ToList();
            }

            if (request.StartDate != null)
            {
                wallItems = wallItems.Where(c => c.EntryDate >= request.StartDate).ToList();
            }

            return wallItems;
        }

        public static CustomerStatus GetCustomerStatus(int customerStatusID)
        {
            var customerStatus = new CustomerStatus();
            using (var context = Exigo.Sql())
            {
                customerStatus = context.Query<CustomerStatus>(@"
                                SELECT CustomerStatusID
                                      ,CustomerStatusDescription
                                FROM CustomerStatuses
                                WHERE CustomerStatusID = @CustomerStatusID
                    ", new
                     {
                         CustomerStatusID = customerStatusID
                     }).FirstOrDefault();
            }

            if (customerStatus == null) return null;

            return customerStatus;
        }
        public static CustomerType GetCustomerType(int customerTypeID)
        {
            var customerType = new CustomerType();
            using (var context = Exigo.Sql())
            {
                customerType = context.Query<CustomerType>(@"
                                SELECT CustomerTypeID
                                      ,CustomerTypeDescription
                                      ,PriceTypeID
                                FROM CustomerTypes
                                WHERE CustomerTypeID = @CustomerTypeID
                    ", new
                     {
                         CustomerTypeID = customerTypeID
                     }).FirstOrDefault();
            }

            if (customerType == null) return null;

            return customerType;
        }

        public static void SetCustomerPreferredLanguage(int customerID, int languageID)
        {
            Exigo.WebService().UpdateCustomer(new UpdateCustomerRequest
            {
                CustomerID = customerID,
                LanguageID = languageID
            });

            var language = Exigo.GetLanguageByID(languageID);
            if (language != null)
            {
                GlobalUtilities.SetCurrentUICulture(language.CultureCode);
            }
        }

        public static bool IsEmailAvailable(int customerID, string email)
        {
            var isEmailAvailable = true;

            var customer = Exigo.WebService().GetCustomers(new GetCustomersRequest()
            {
                Email = email
            }).Customers
            .Where(c => c.CustomerID != customerID).Count();

            if (customer > 0)
            {
                isEmailAvailable = false;
            }

            return isEmailAvailable;
        }
        public static bool IsLoginNameAvailable(string loginname, int customerID = 0)
        {
            if (customerID > 0)
            {
                // Get the current login name to see if it matches what we passed. If so, it's still valid.
                var currentLoginName = Exigo.GetCustomer(customerID).LoginName;
                if (loginname.Equals(currentLoginName, StringComparison.InvariantCultureIgnoreCase)) return true;
            }

            // Validate the login name
            // cannot use SQL due to delay in update to replicated database
            var apiCustomer = Exigo.WebService().GetCustomers(new GetCustomersRequest()
            {
                LoginName = loginname
            }).Customers.FirstOrDefault();

            return (apiCustomer == null);
        }

        public static void SendEmailVerification(int customerID, string email)
        {
            // Create the publicly-accessible verification link
            string sep = "&";
            if (!GlobalSettings.Emails.VerifyEmailUrl.Contains("?")) sep = "?";

            string encryptedValues = Security.Encrypt(new
            {
                CustomerID = customerID,
                Email = email,
                Date = DateTime.Now
            });

            var verifyEmailUrl = GlobalSettings.Emails.VerifyEmailUrl + sep + "token=" + encryptedValues;


            // Send the email
            Exigo.SendEmail(new SendEmailRequest
            {
                To = new[] { email },
                From = GlobalSettings.Emails.NoReplyEmail,
                ReplyTo = new[] { GlobalSettings.Emails.NoReplyEmail },
                SMTPConfiguration = GlobalSettings.Emails.SMTPConfigurations.Default,
                Subject = "{0} - Verify your email".FormatWith(GlobalSettings.Company.Name),
                Body = @"
                    <p>
                        {1} has received a request to enable this email account to receive email notifications from {1} and your upline.
                    </p>

                    <p> 
                        To confirm this email account, please click the following link:<br />
                        <a href='{0}'>{0}</a>
                    </p>

                    <p>
                        If you did not request email notifications from {1}, or believe you have received this email in error, please contact {1} customer service.
                    </p>

                    <p>
                        Sincerely, <br />
                        {1} Customer Service
                    </p>"
                    .FormatWith(verifyEmailUrl, GlobalSettings.Company.Name)
            });
        }
        public static void OptInCustomer(string token)
        {
            var decryptedToken = Security.Decrypt(token);

            var customerID = Convert.ToInt32(decryptedToken.CustomerID);
            var email = decryptedToken.Email.ToString();

            OptInCustomer(customerID, email);
        }
        public static void OptInCustomer(int customerID, string email)
        {
            Exigo.WebService().UpdateCustomer(new UpdateCustomerRequest
            {
                CustomerID = customerID,
                Email = email,
                SubscribeToBroadcasts = true,
                SubscribeFromIPAddress = GlobalUtilities.GetClientIP()
            });
        }
        public static void OptOutCustomer(int customerID)
        {
            Exigo.WebService().UpdateCustomer(new UpdateCustomerRequest
            {
                CustomerID = customerID,
                SubscribeToBroadcasts = false
            });
        }

        //Only Used for the Dashboard Card... Really all this does is return the customerID's then lets the customer model pull the avatar URL.
        public static List<Customer> GetNewestDistributors(GetNewestDistributorsRequest request)
        {
            var newestDistributors = new List<Customer>();
            var customerTypes = request.CustomerTypes;
            var customerStatuses = request.CustomerStatuses;
            using (var context = Exigo.Sql())
            {
                newestDistributors = context.Query<Customer>(@"
                                SELECT TOP (@RowCount) un.DownlineCustomerID
	                                  , c.CustomerID
                                      , c.FirstName
                                      , c.MiddleName
                                      , c.LastName
                                      , c.CreatedDate
                                FROM UniLevelDownline un
	                                LEFT JOIN Customers c
		                                ON un.CustomerID = c.CustomerID
                                WHERE un.DownlineCustomerID = @CustomerID
                                    AND c.CustomerID <> @CustomerID
	                                AND un.Level <= @Level
                                    AND c.CustomerTypeID IN @CustomerTypes
                                    AND c.CustomerStatusID IN @CustomerStatuses
                                    AND c.CreatedDate >= CASE 
                                        WHEN @days > 0 
                                        THEN getdate()-@Days 
                                        ELSE c.CreatedDate 
                                        END
                                ORDER BY CreatedDate
                    ", new
                     {
                        CustomerID       = request.CustomerID,
                        Level            = request.MaxLevel,
                        RowCount         = request.RowCount,
                        CustomerTypes    = customerTypes,
                        CustomerStatuses = customerStatuses,
                        Days             = request.Days

                     }).ToList();
            }

            return newestDistributors;
        }
    }
}
