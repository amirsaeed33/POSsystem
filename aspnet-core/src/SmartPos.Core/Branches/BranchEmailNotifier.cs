using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Services;
using Abp.Net.Mail;
using Castle.Core.Logging;

namespace SmartPos.Branches
{
    public class BranchEmailNotifier : DomainService, IBranchEmailNotifier
    {
        private readonly IEmailSender _emailSender;

        public BranchEmailNotifier(IEmailSender emailSender)
        {
            _emailSender = emailSender;
            Logger = NullLogger.Instance;
        }

        public async Task SendEmailsAsync(IEnumerable<string> emails, string subject, string body)
        {
            if (emails == null) return;

            foreach (var email in emails.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()))
            {
                try
                {
                    await _emailSender.SendAsync(email, subject, body);
                }
                catch (System.Exception ex)
                {
                    Logger.Error($"Failed to send notification email to {email}", ex);
                }
            }
        }

        public async Task SendEmailsAsync(string emailsCsv, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(emailsCsv)) return;

            var emails = emailsCsv.Split(',');
            await SendEmailsAsync(emails, subject, body);
        }
    }
}
