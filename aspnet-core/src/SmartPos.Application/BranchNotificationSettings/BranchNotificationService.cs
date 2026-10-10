using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Castle.Core.Logging;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Emailing;

namespace SmartPos.BranchNotificationSettings
{
    public class BranchNotificationService : IBranchNotificationService, ITransientDependency
    {
        private readonly IIocResolver _iocResolver;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public ILogger Logger { get; set; }

        public BranchNotificationService(
            IIocResolver iocResolver,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _iocResolver = iocResolver;
            _unitOfWorkManager = unitOfWorkManager;
            Logger = NullLogger.Instance;
        }

        public void SendNotificationAfterCommit(
            BranchNotificationType type,
            int? tenantId,
            int branchId,
            string referenceNo,
            decimal totalAmount,
            long? creatorUserId,
            DateTime dateTime)
        {
            try
            {
                var currentUow = _unitOfWorkManager.Current;
                if (currentUow != null)
                {
                    currentUow.Completed += (sender, args) =>
                    {
                        _ = Task.Run(() => ExecuteSendNotificationAsync(type, tenantId, branchId, referenceNo, totalAmount, creatorUserId, dateTime));
                    };
                }
                else
                {
                    _ = Task.Run(() => ExecuteSendNotificationAsync(type, tenantId, branchId, referenceNo, totalAmount, creatorUserId, dateTime));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[BranchNotification] Failed to register notification trigger for {type} {referenceNo}: {ex.Message}", ex);
            }
        }

        private async Task ExecuteSendNotificationAsync(
            BranchNotificationType type,
            int? tenantId,
            int branchId,
            string referenceNo,
            decimal totalAmount,
            long? creatorUserId,
            DateTime dateTime)
        {
            try
            {
                using (var uowManagerWrapper = _iocResolver.ResolveAsDisposable<IUnitOfWorkManager>())
                {
                    var uowManager = uowManagerWrapper.Object;
                    using (var uow = uowManager.Begin())
                    {
                        using (uowManager.Current.SetTenantId(tenantId))
                        {
                            using (var settingRepoWrapper = _iocResolver.ResolveAsDisposable<IRepository<BranchNotificationSetting>>())
                            {
                                var setting = await settingRepoWrapper.Object.FirstOrDefaultAsync(
                                    s => s.BranchId == branchId && s.TenantId == tenantId);

                                if (setting == null || !setting.IsEnabled)
                                {
                                    return;
                                }

                                bool shouldNotify = type switch
                                {
                                    BranchNotificationType.Sale => setting.NotifyOnSale,
                                    BranchNotificationType.Purchase => setting.NotifyOnPurchase,
                                    BranchNotificationType.Expense => setting.NotifyOnExpense,
                                    _ => false
                                };

                                if (!shouldNotify || string.IsNullOrWhiteSpace(setting.Emails))
                                {
                                    return;
                                }

                                var recipients = setting.Emails
                                    .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(e => e.Trim())
                                    .Where(e => !string.IsNullOrEmpty(e))
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToList();

                                if (!recipients.Any())
                                {
                                    return;
                                }

                                string branchName = null;
                                using (var branchRepoWrapper = _iocResolver.ResolveAsDisposable<IRepository<Branch>>())
                                {
                                    var branch = await branchRepoWrapper.Object.FirstOrDefaultAsync(branchId);
                                    branchName = branch?.Name;
                                }
                                if (string.IsNullOrWhiteSpace(branchName))
                                {
                                    branchName = $"Branch #{branchId}";
                                }

                                string creatorName = null;
                                if (creatorUserId.HasValue)
                                {
                                    using (var userRepoWrapper = _iocResolver.ResolveAsDisposable<IRepository<User, long>>())
                                    {
                                        var user = await userRepoWrapper.Object.FirstOrDefaultAsync(creatorUserId.Value);
                                        if (user != null)
                                        {
                                            creatorName = $"{user.Name} {user.Surname}".Trim();
                                            if (string.IsNullOrWhiteSpace(creatorName))
                                            {
                                                creatorName = user.UserName;
                                            }
                                        }
                                    }
                                }
                                if (string.IsNullOrWhiteSpace(creatorName))
                                {
                                    creatorName = "System";
                                }

                                var (subject, bodyHtml) = await BuildNotificationEmailHtmlAsync(type, branchName, referenceNo, totalAmount, creatorName, dateTime, tenantId);

                                using (var mailSenderWrapper = _iocResolver.ResolveAsDisposable<ISmtpMailSender>())
                                {
                                    foreach (var recipient in recipients)
                                    {
                                        try
                                        {
                                            await mailSenderWrapper.Object.SendAsync(recipient, subject, bodyHtml, isBodyHtml: true);
                                        }
                                        catch (Exception sendEx)
                                        {
                                            Logger.Error($"[BranchNotification] Error sending {type} notification to '{recipient}': {sendEx.Message}", sendEx);
                                        }
                                    }
                                }
                            }

                            await uow.CompleteAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[BranchNotification] Error during notification execution for {type} {referenceNo} (Branch: {branchId}): {ex.Message}", ex);
            }
        }

        private async Task<(string Subject, string BodyHtml)> BuildNotificationEmailHtmlAsync(
            BranchNotificationType type,
            string branchName,
            string referenceNo,
            decimal totalAmount,
            string creatorName,
            DateTime dateTime,
            int? tenantId)
        {
            string typeName;
            string primaryColor;
            string badgeBg;
            string badgeColor;
            string titleEmoji;
            string templateCode;

            switch (type)
            {
                case BranchNotificationType.Sale:
                    typeName = "Sale";
                    primaryColor = "#10b981";
                    badgeBg = "#d1fae5";
                    badgeColor = "#065f46";
                    titleEmoji = "🛒";
                    templateCode = EmailTemplateCodes.SaleCreated;
                    break;
                case BranchNotificationType.Purchase:
                    typeName = "Purchase";
                    primaryColor = "#6366f1";
                    badgeBg = "#e0e7ff";
                    badgeColor = "#3730a3";
                    titleEmoji = "📦";
                    templateCode = EmailTemplateCodes.PurchaseCreated;
                    break;
                case BranchNotificationType.Expense:
                    typeName = "Expense";
                    primaryColor = "#f59e0b";
                    badgeBg = "#fef3c7";
                    badgeColor = "#92400e";
                    titleEmoji = "💸";
                    templateCode = EmailTemplateCodes.ExpenseCreated;
                    break;
                default:
                    typeName = type.ToString();
                    primaryColor = "#2563eb";
                    badgeBg = "#dbeafe";
                    badgeColor = "#1e40af";
                    titleEmoji = "🔔";
                    templateCode = EmailTemplateCodes.SaleCreated;
                    break;
            }

            var safeBranchName = WebUtility.HtmlEncode(branchName ?? "Unknown Location");
            var safeRefNo = WebUtility.HtmlEncode(referenceNo ?? "N/A");
            var safeCreatorName = WebUtility.HtmlEncode(creatorName ?? "System");
            var formattedDate = dateTime.ToString("yyyy-MM-dd hh:mm tt");

            EmailTemplate template = null;
            using (var templateRepoWrapper = _iocResolver.ResolveAsDisposable<IRepository<EmailTemplate>>())
            {
                template = await templateRepoWrapper.Object.FirstOrDefaultAsync(x => x.Code == templateCode && x.TenantId == tenantId && x.IsActive);
                if (template == null && tenantId.HasValue)
                {
                    // Fallback to host template
                    template = await templateRepoWrapper.Object.FirstOrDefaultAsync(x => x.Code == templateCode && x.TenantId == null && x.IsActive);
                }
            }

            var subjectTemplate = template?.Subject ?? $"[SmartPOS] New {typeName} Alert - {safeRefNo} ({safeBranchName})";
            var bodyTemplate = template?.BodyHtml;

            if (string.IsNullOrWhiteSpace(bodyTemplate))
            {
                bodyTemplate = EmailTemplateDefaults.TransactionCreatedBodyHtml();
            }

            var placeholders = new Dictionary<string, string>
            {
                { "TypeName", typeName },
                { "BranchName", safeBranchName },
                { "ReferenceNo", safeRefNo },
                { "TotalAmount", totalAmount.ToString("N2") },
                { "CreatorName", safeCreatorName },
                { "FormattedDate", formattedDate },
                { "PrimaryColor", primaryColor },
                { "BadgeBg", badgeBg },
                { "BadgeColor", badgeColor },
                { "TitleEmoji", titleEmoji }
            };

            return (
                EmailTemplateRenderer.Render(subjectTemplate, placeholders),
                EmailTemplateRenderer.Render(bodyTemplate, placeholders)
            );
        }
    }
}
