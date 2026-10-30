using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Abp.Dependency;
using Castle.Core.Logging;
using Microsoft.Extensions.Configuration;
using SmartPos.Emailing;

namespace SmartPos.Net.Emailing
{
    public class SmtpMailSender : ISmtpMailSender, ITransientDependency
    {
        private readonly IConfiguration _configuration;
        public ILogger Logger { get; set; }

        public SmtpMailSender(IConfiguration configuration)
        {
            _configuration = configuration;
            Logger = NullLogger.Instance;
        }

        public async Task SendAsync(string toAddress, string subject, string body, bool isBodyHtml = false, string fromDisplayName = null)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                Logger.Warn("SendAsync called with empty toAddress.");
                return;
            }

            var host = _configuration["Email:SmtpHost"];
            var userName = _configuration["Email:UserName"];
            var password = _configuration["Email:Password"];
            var fromAddress = _configuration["Email:FromAddress"] ?? userName;
            var resolvedFromDisplayName = !string.IsNullOrWhiteSpace(fromDisplayName)
                ? fromDisplayName.Trim()
                : (_configuration["Email:FromDisplayName"] ?? "SmartPos");

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromAddress))
            {
                Logger.Warn($"Email to '{toAddress}' was not sent because SMTP is not configured in appsettings.json. Subject: '{subject}'");
                return;
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
                    message.From = new MailAddress(fromAddress, resolvedFromDisplayName);
                    message.To.Add(new MailAddress(toAddress.Trim()));
                    message.Subject = subject ?? string.Empty;
                    message.IsBodyHtml = isBodyHtml;
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
            catch (Exception ex)
            {
                Logger.Error($"Failed to send email to '{toAddress}': {ex.Message}", ex);
            }
        }
    }
}
