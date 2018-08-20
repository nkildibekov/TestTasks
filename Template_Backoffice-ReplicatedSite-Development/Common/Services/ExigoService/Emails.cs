using Common.Api.ExigoWebService;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ExigoService
{
    public static partial class Exigo
    {
        public static void SendEmail(SendEmailRequest request)
        {
            if (request.SMTPConfiguration == null && !request.UseExigoApi)
            {
                return;
            }

            if (request.UseExigoApi)
            {
                var sendEmailRequest = new Common.Api.ExigoWebService.SendEmailRequest();

                sendEmailRequest.MailFrom = request.From;
                sendEmailRequest.Subject  = request.Subject;
                sendEmailRequest.Body     = request.Body;
                
                // Send the emails
                foreach (var email in request.To)
                {
                    Task.Factory.StartNew(() =>
                    {
                        sendEmailRequest.MailTo = email;
                        Exigo.WebService().SendEmail(sendEmailRequest);
                    });
                }
            }
            else
            {
                // Create a list of emails we will send
                var emails = new List<MailMessage>();

                // SMTP Credentials
                SmtpClient smtp = new SmtpClient(request.SMTPConfiguration.Server, request.SMTPConfiguration.Port);
                smtp.Credentials = new System.Net.NetworkCredential(request.SMTPConfiguration.Username, request.SMTPConfiguration.Password);
                smtp.EnableSsl = request.SMTPConfiguration.EnableSSL;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                // Send an email to each recipient
                foreach (var recipientEmail in request.To)
                {
                    // Create the MailMessage object
                    MailMessage email = new MailMessage(request.From, recipientEmail);
                    email.Priority = request.Priority;
                    email.IsBodyHtml = request.IsHtml;

                    // Reply to
                    foreach (var replyTo in request.ReplyTo)
                    {
                        email.ReplyToList.Add(replyTo);
                    }

                    // Subject and body
                    email.Subject = request.Subject;
                    email.Body = request.Body;

                    // Add the email to the collection
                    emails.Add(email);
                }

                // Send the emails
                foreach (var email in emails)
                {
                    Task.Factory.StartNew(() =>
                    {
                        smtp.Send(email);
                    });
                }
            }
        }
    }
}