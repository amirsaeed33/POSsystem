using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartPos.Emailing;

namespace SmartPos.EntityFrameworkCore.Seed.Host
{
    public class DefaultEmailTemplatesCreator
    {
        private readonly SmartPosDbContext _context;
        private readonly int? _tenantId;

        public DefaultEmailTemplatesCreator(SmartPosDbContext context, int? tenantId)
        {
            _context = context;
            _tenantId = tenantId;
        }

        public void Create()
        {
            EnsureEmailLoginCodeTemplate();
            _context.SaveChanges();
        }

        private void EnsureEmailLoginCodeTemplate()
        {
            var exists = _context.EmailTemplates
                .IgnoreQueryFilters()
                .Any(x => x.TenantId == _tenantId
                          && x.Code == EmailTemplateCodes.EmailLoginCode
                          && !x.IsDeleted);

            if (exists)
            {
                return;
            }

            _context.EmailTemplates.Add(new EmailTemplate
            {
                TenantId = _tenantId,
                Name = "Email sign-in code",
                Code = EmailTemplateCodes.EmailLoginCode,
                Subject = "Your {{AppName}} sign-in code",
                Description = "Sent when a user requests a passwordless email login code. Placeholders: {{Code}}, {{ExpirationMinutes}}, {{UserName}}, {{Name}}, {{Email}}, {{AppName}}.",
                IsActive = true,
                BodyHtml = EmailTemplateDefaults.EmailLoginCodeBodyHtml()
            });
        }

        public static string DefaultEmailLoginCodeBodyHtml()
        {
            return EmailTemplateDefaults.EmailLoginCodeBodyHtml();
        }
    }
}
