using Common.Api.ExigoWebService;
using AdminDashboard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using ExigoService;
using Common;

namespace AdminDashboard.Services
{
    public class IdentityService
    {
        public IdentityService() { }

        public LoginResponse SignIn(string loginname, string password)
        {
            var response = new LoginResponse();

            try
            {
                // Authenticate the customer
                var authenticateUserResponse = Exigo.WebService().AuthenticateUser(new AuthenticateUserRequest
                {
                    LoginName = loginname,
                    Password = password
                });

                if (authenticateUserResponse.Result.Status == ResultStatus.Failure)
                {
                    response.Fail("Unable to authenticate");
                    return response;
                }

                CreateFormsAuthenticationTicket(loginname, authenticateUserResponse);


                // Mark the response as successful
                response.Success();
            }
            catch (Exception ex)
            {
                response.Fail(ex.Message);
            }

            return response;
        }
        public void SignOut()
        {
            FormsAuthentication.SignOut();
        }

        public bool CreateFormsAuthenticationTicket(string loginName, AuthenticateUserResponse response)
        {
            // If we got here, we are authorized. Let's attempt to get the identity.
            var identity = new UserIdentity
            {
                FirstName = response.FirstName,
                LastName = response.LastName,
                LoginName = loginName
            }; 


            // Create the ticket
            FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                1,
                GlobalSettings.Exigo.Api.CompanyKey + "_" + loginName,
                DateTime.Now,
                DateTime.Now.AddMinutes(GlobalSettings.Backoffices.SessionTimeout),
                false,
                identity.SerializeProperties());


            // Encrypt the ticket
            string encTicket = FormsAuthentication.Encrypt(ticket);


            // Create the cookie.
            HttpCookie cookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName]; //saved user
            if (cookie == null)
            {
                HttpContext.Current.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, encTicket));
            }
            else
            {
                cookie.Value = encTicket;
                HttpContext.Current.Response.Cookies.Set(cookie);
            }


            return true;
        }
    }
}