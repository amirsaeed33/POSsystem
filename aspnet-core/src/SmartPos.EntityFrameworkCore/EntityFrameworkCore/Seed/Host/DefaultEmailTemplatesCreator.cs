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
            EnsureBranchActivationTemplate();
            EnsureSaleCreatedTemplate();
            EnsurePurchaseCreatedTemplate();
            EnsureExpenseCreatedTemplate();
            EnsureOnlineOrderCreatedTemplate();
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

        private void EnsureBranchActivationTemplate()
        {
            var exists = _context.EmailTemplates
                .IgnoreQueryFilters()
                .Any(x => x.TenantId == _tenantId
                          && x.Code == EmailTemplateCodes.BranchActivation
                          && !x.IsDeleted);

            if (exists)
            {
                return;
            }

            _context.EmailTemplates.Add(new EmailTemplate
            {
                TenantId = _tenantId,
                Name = "Branch activation",
                Code = EmailTemplateCodes.BranchActivation,
                Subject = "Activate {{BranchName}} for {{TenantName}}",
                Description = "Sent when a host admin approves a branch. Placeholders: {{TenantName}}, {{BranchName}}, {{ActivationLink}}, {{AppName}}, {{ExpirationHours}}.",
                IsActive = true,
                BodyHtml = EmailTemplateDefaults.BranchActivationBodyHtml()
            });
        }

        private void EnsureSaleCreatedTemplate()
        {
            var exists = _context.EmailTemplates.IgnoreQueryFilters().Any(x => x.TenantId == _tenantId && x.Code == EmailTemplateCodes.SaleCreated && !x.IsDeleted);
            if (exists) return;
            _context.EmailTemplates.Add(new EmailTemplate { TenantId = _tenantId, Name = "Sale created", Code = EmailTemplateCodes.SaleCreated, Subject = "New Sale Created - {{ReferenceNo}} ({{BranchName}})", Description = "Sent when a sale is created. Placeholders: {{TypeName}}, {{BranchName}}, {{ReferenceNo}}, {{TotalAmount}}, {{CreatorName}}, {{FormattedDate}}, {{PrimaryColor}}, {{BadgeBg}}, {{BadgeColor}}, {{TitleEmoji}}", IsActive = true, BodyHtml = EmailTemplateDefaults.TransactionCreatedBodyHtml() });
        }

        private void EnsurePurchaseCreatedTemplate()
        {
            var exists = _context.EmailTemplates.IgnoreQueryFilters().Any(x => x.TenantId == _tenantId && x.Code == EmailTemplateCodes.PurchaseCreated && !x.IsDeleted);
            if (exists) return;
            _context.EmailTemplates.Add(new EmailTemplate { TenantId = _tenantId, Name = "Purchase created", Code = EmailTemplateCodes.PurchaseCreated, Subject = "New Purchase Created - {{ReferenceNo}} ({{BranchName}})", Description = "Sent when a purchase is created. Placeholders: {{TypeName}}, {{BranchName}}, {{ReferenceNo}}, {{TotalAmount}}, {{CreatorName}}, {{FormattedDate}}, {{PrimaryColor}}, {{BadgeBg}}, {{BadgeColor}}, {{TitleEmoji}}", IsActive = true, BodyHtml = EmailTemplateDefaults.TransactionCreatedBodyHtml() });
        }

        private void EnsureExpenseCreatedTemplate()
        {
            var exists = _context.EmailTemplates.IgnoreQueryFilters().Any(x => x.TenantId == _tenantId && x.Code == EmailTemplateCodes.ExpenseCreated && !x.IsDeleted);
            if (exists) return;
            _context.EmailTemplates.Add(new EmailTemplate { TenantId = _tenantId, Name = "Expense created", Code = EmailTemplateCodes.ExpenseCreated, Subject = "New Expense Created - {{ReferenceNo}} ({{BranchName}})", Description = "Sent when an expense is created. Placeholders: {{TypeName}}, {{BranchName}}, {{ReferenceNo}}, {{TotalAmount}}, {{CreatorName}}, {{FormattedDate}}, {{PrimaryColor}}, {{BadgeBg}}, {{BadgeColor}}, {{TitleEmoji}}", IsActive = true, BodyHtml = EmailTemplateDefaults.TransactionCreatedBodyHtml() });
        }

        private void EnsureOnlineOrderCreatedTemplate()
        {
            var exists = _context.EmailTemplates.IgnoreQueryFilters().Any(x => x.TenantId == _tenantId && x.Code == EmailTemplateCodes.OnlineOrderCreated && !x.IsDeleted);
            if (exists) return;
            _context.EmailTemplates.Add(new EmailTemplate { TenantId = _tenantId, Name = "Online order created", Code = EmailTemplateCodes.OnlineOrderCreated, Subject = "New Online Order Received - {{OrderNo}} ({{BranchName}})", Description = "Sent when a new online order is created. Placeholders: {{BranchName}}, {{OrderNo}}, {{CustomerName}}, {{TotalAmount}}, {{FormattedDate}}", IsActive = true, BodyHtml = EmailTemplateDefaults.OnlineOrderCreatedBodyHtml() });
        }

        public static string DefaultEmailLoginCodeBodyHtml()
        {
            return EmailTemplateDefaults.EmailLoginCodeBodyHtml();
        }
    }
}
