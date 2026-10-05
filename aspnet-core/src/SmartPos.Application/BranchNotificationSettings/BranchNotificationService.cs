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

                                var (subject, bodyHtml) = BuildNotificationEmailHtml(type, branchName, referenceNo, totalAmount, creatorName, dateTime);

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

        private static (string Subject, string BodyHtml) BuildNotificationEmailHtml(
            BranchNotificationType type,
            string branchName,
            string referenceNo,
            decimal totalAmount,
            string creatorName,
            DateTime dateTime)
        {
            string typeName;
            string primaryColor;
            string badgeBg;
            string badgeColor;
            string titleEmoji;

            switch (type)
            {
                case BranchNotificationType.Sale:
                    typeName = "Sale";
                    primaryColor = "#10b981"; // Emerald
                    badgeBg = "#d1fae5";
                    badgeColor = "#065f46";
                    titleEmoji = "🛒";
                    break;
                case BranchNotificationType.Purchase:
                    typeName = "Purchase";
                    primaryColor = "#6366f1"; // Indigo
                    badgeBg = "#e0e7ff";
                    badgeColor = "#3730a3";
                    titleEmoji = "📦";
                    break;
                case BranchNotificationType.Expense:
                    typeName = "Expense";
                    primaryColor = "#f59e0b"; // Amber
                    badgeBg = "#fef3c7";
                    badgeColor = "#92400e";
                    titleEmoji = "💸";
                    break;
                default:
                    typeName = type.ToString();
                    primaryColor = "#2563eb";
                    badgeBg = "#dbeafe";
                    badgeColor = "#1e40af";
                    titleEmoji = "🔔";
                    break;
            }

            var safeBranchName = WebUtility.HtmlEncode(branchName ?? "Unknown Location");
            var safeRefNo = WebUtility.HtmlEncode(referenceNo ?? "N/A");
            var safeCreatorName = WebUtility.HtmlEncode(creatorName ?? "System");
            var formattedDate = dateTime.ToString("yyyy-MM-dd hh:mm tt");

            var subject = $"[SmartPOS] New {typeName} Alert - {safeRefNo} ({safeBranchName})";

            var sb = new StringBuilder();
            sb.Append($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08); }}
        .header {{ background: {primaryColor}; padding: 24px; color: #ffffff; text-align: center; }}
        .header h2 {{ margin: 0; font-size: 22px; font-weight: 600; }}
        .header p {{ margin: 6px 0 0 0; opacity: 0.9; font-size: 14px; }}
        .content {{ padding: 24px; }}
        .badge {{ background: {badgeBg}; color: {badgeColor}; font-size: 13px; font-weight: 600; padding: 4px 12px; border-radius: 12px; display: inline-block; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 14px; }}
        th {{ background: #f8fafc; color: #475569; text-align: left; padding: 12px 14px; border-bottom: 2px solid #e2e8f0; font-weight: 600; width: 35%; }}
        td {{ padding: 12px 14px; border-bottom: 1px solid #f1f5f9; color: #1e293b; }}
        .amount {{ color: {primaryColor}; font-weight: bold; font-size: 16px; }}
        .footer {{ background: #f8fafc; padding: 16px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{titleEmoji} New {typeName} Created</h2>
            <p>{safeBranchName} · {formattedDate}</p>
        </div>
        <div class='content'>
            <p>A new <strong>{typeName}</strong> transaction has been successfully recorded in <strong>{safeBranchName}</strong>.</p>
            <table>
                <tbody>
                    <tr>
                        <th>Transaction Type</th>
                        <td><span class='badge'>{typeName}</span></td>
                    </tr>
                    <tr>
                        <th>Branch Location</th>
                        <td><strong>{safeBranchName}</strong></td>
                    </tr>
                    <tr>
                        <th>Reference / Invoice</th>
                        <td><strong style='font-family: monospace; font-size: 14px;'>{safeRefNo}</strong></td>
                    </tr>
                    <tr>
                        <th>Total Amount</th>
                        <td class='amount'>{totalAmount:N2}</td>
                    </tr>
                    <tr>
                        <th>Recorded By</th>
                        <td>{safeCreatorName}</td>
                    </tr>
                    <tr>
                        <th>Date & Time</th>
                        <td>{formattedDate}</td>
                    </tr>
                </tbody>
            </table>
        </div>
        <div class='footer'>
            Sent automatically by <strong>SmartPOS System</strong> for branch: <strong>{safeBranchName}</strong>.
        </div>
    </div>
</body>
</html>");

            return (subject, sb.ToString());
        }
    }
}
