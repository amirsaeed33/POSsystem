using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.UI;
using Microsoft.Extensions.Configuration;

namespace SmartPos.Net.Emailing
{
    public class SmtpMailSender : ISmtpMailSender, ITransientDependency
    {
        private readonly IConfiguration _configuration;

        public SmtpMailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string toAddress, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                throw new UserFriendlyException("Recipient email is required.");
            }

            var host = _configuration["Email:SmtpHost"];
            var userName = _configuration["Email:UserName"];
            var password = _configuration["Email:Password"];
            var fromAddress = _configuration["Email:FromAddress"] ?? userName;
            var fromDisplayName = _configuration["Email:FromDisplayName"] ?? "SmartPos";

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromAddress))
            {
                throw new UserFriendlyException("Email is not configured on the server.");
            }

            var port = 587;
            int.TryParse(_configuration["Email:SmtpPort"], out port);
            if (port <= 0)
            {
                port = 587;
            }

            var enableSsl = !string.Equals(
                _configuration["Email:EnableSsl"],
                "false",
                StringComparison.OrdinalIgnoreCase);

            try
            {
                using (var message = new MailMessage())
                using (var smtp = new SmtpClient())
                {
                    message.From = new MailAddress(fromAddress, fromDisplayName);
                    message.To.Add(new MailAddress(toAddress.Trim()));
                    message.Subject = subject ?? string.Empty;
                    message.IsBodyHtml = false;
                    message.Body = body ?? string.Empty;

                    smtp.Host = host;
                    smtp.Port = port;
                    smtp.EnableSsl = enableSsl;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(userName, password);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    await smtp.SendMailAsync(message);
                }
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new UserFriendlyException("Failed to send email. Please try again later.");
            }
        }
    }
}
