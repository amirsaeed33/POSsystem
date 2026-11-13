using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Timing;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartPos.Authorization;
using SmartPos.Billing.Dto;
using SmartPos.Emailing;
using SmartPos.MultiTenancy;
using SmartPos.Packages;

namespace SmartPos.Billing
{
    [AbpAuthorize]
    public class SubscriptionBillingAppService : ApplicationService, ISubscriptionBillingAppService
    {
        public const int RenewalWindowDays = 7;
        public const int MaxQrBytes = 1024 * 1024;
        public const int MaxReceiptBytes = 2 * 1024 * 1024;
        public const string ApprovalEmail = "amirdev639@gmail.com";

        private readonly IRepository<PaymentMethod> _paymentMethods;
        private readonly IRepository<TenantSubscription> _subscriptions;
        private readonly IRepository<SubscriptionPayment> _payments;
        private readonly IRepository<Package> _packages;
        private readonly IRepository<Tenant> _tenants;
        private readonly ISmtpMailSender _mailSender;
        private readonly IConfiguration _configuration;

        public SubscriptionBillingAppService(
            IRepository<PaymentMethod> paymentMethods,
            IRepository<TenantSubscription> subscriptions,
            IRepository<SubscriptionPayment> payments,
            IRepository<Package> packages,
            IRepository<Tenant> tenants,
            ISmtpMailSender mailSender,
            IConfiguration configuration)
        {
            _paymentMethods = paymentMethods;
            _subscriptions = subscriptions;
            _payments = payments;
            _packages = packages;
            _tenants = tenants;
            _mailSender = mailSender;
            _configuration = configuration;
        }

        [AbpAuthorize(PermissionNames.Pages_Subscriptions)]
        public virtual async Task<List<PaymentMethodDto>> GetPaymentMethods()
        {
            var rows = await _paymentMethods.GetAll()
                .OrderBy(x => x.Kind)
                .ToListAsync();
            return rows.Select(x => MapMethod(x, includeQr: true)).ToList();
        }

        [AbpAuthorize(PermissionNames.Pages_Subscriptions)]
        public virtual async Task<PaymentMethodDto> SavePaymentMethod(SavePaymentMethodInput input)
        {
            if (input == null)
            {
                throw new UserFriendlyException("Payment method is required.");
            }

            var title = (input.AccountTitle ?? string.Empty).Trim();
            var number = (input.AccountNumber ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(number))
            {
                throw new UserFriendlyException("Account title and number are required.");
            }

            if (!Enum.IsDefined(typeof(PaymentMethodKind), input.Kind))
            {
                throw new UserFriendlyException("Choose Easypaisa, JazzCash, Nayapay, or a bank account.");
            }

            var entity = input.Id.HasValue
                ? await _paymentMethods.FirstOrDefaultAsync(input.Id.Value)
                : null;
            if (input.Id.HasValue && entity == null)
            {
                throw new UserFriendlyException("Payment method was not found.");
            }

            var duplicate = await _paymentMethods.GetAll()
                .FirstOrDefaultAsync(x => x.Kind == input.Kind && (entity == null || x.Id != entity.Id));
            if (duplicate != null)
            {
                throw new UserFriendlyException(KindName(input.Kind) + " is already set up. Edit that row.");
            }

            if (entity == null)
            {
                entity = new PaymentMethod();
            }

            entity.Kind = input.Kind;
            entity.AccountTitle = title;
            entity.AccountNumber = number;
            entity.BankName = string.IsNullOrWhiteSpace(input.BankName) ? null : input.BankName.Trim();
            entity.Instructions = string.IsNullOrWhiteSpace(input.Instructions) ? null : input.Instructions.Trim();
            entity.IsActive = input.IsActive;

            if (input.ClearQr)
            {
                entity.QrImage = null;
                entity.QrContentType = null;
            }
            else if (!string.IsNullOrWhiteSpace(input.QrBase64))
            {
                var image = DecodeImage(input.QrBase64, input.QrContentType, MaxQrBytes, "QR code");
                entity.QrImage = image.Bytes;
                entity.QrContentType = image.ContentType;
            }
            else if (entity.Id == 0)
            {
                entity.QrImage = null;
                entity.QrContentType = null;
            }

            if (entity.Id == 0)
            {
                await _paymentMethods.InsertAsync(entity);
            }
            else
            {
                await _paymentMethods.UpdateAsync(entity);
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            return MapMethod(entity, includeQr: true);
        }

        [AbpAuthorize(PermissionNames.Pages_Subscriptions)]
        public virtual async Task DeletePaymentMethod(int id)
        {
            var entity = await _paymentMethods.FirstOrDefaultAsync(id);
            if (entity == null)
            {
                throw new UserFriendlyException("Payment method was not found.");
            }

            await _paymentMethods.DeleteAsync(entity);
        }

        [AbpAuthorize(PermissionNames.Pages_Subscriptions)]
        public virtual async Task<List<AssignablePlanDto>> GetAssignablePlans()
        {
            return await _packages.GetAll()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .Select(x => new AssignablePlanDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    MonthlyAmount = x.MonthlyAmount,
                    YearlyAmount = x.YearlyAmount
                })
                .ToListAsync();
        }

        [AbpAuthorize(PermissionNames.Pages_Billing)]
        public virtual async Task<List<AssignablePlanDto>> GetAvailablePlans()
        {
            return await LoadAvailablePlans();
        }

        [AbpAuthorize(PermissionNames.Pages_Subscriptions)]
        public virtual async Task<List<TenantSubscriptionDto>> GetSubscriptions()
        {
            var rows = await _subscriptions.GetAll().ToListAsync();
            var pendingTenantIds = await PendingTenantIds();
            var tenants = await _tenants.GetAll().ToListAsync();
            return rows
                .Select(x => MapSubscription(x, pendingTenantIds.Contains(x.TenantId), tenants))
                .OrderBy(x => x.TenantName)
                .ToList();
        }

        [AbpAuthorize(PermissionNames.Pages_Subscriptions)]
        public virtual async Task<TenantSubscriptionDto> GetTenantSubscription(int tenantId)
        {
            await EnsureTenantExists(tenantId);
            var row = await _subscriptions.GetAll().FirstOrDefaultAsync(x => x.TenantId == tenantId);
            var pending = await HasPending(tenantId);
            var tenants = await _tenants.GetAll().Where(x => x.Id == tenantId).ToListAsync();
            return row == null
                ? EmptySubscription(tenantId, tenants)
                : MapSubscription(row, pending, tenants);
        }

        [AbpAuthorize(PermissionNames.Pages_Subscriptions)]
        public virtual async Task<TenantSubscriptionDto> SaveTenantSubscription(SaveTenantSubscriptionInput input)
        {
            if (input == null || input.TenantId <= 0)
            {
                throw new UserFriendlyException("Choose a business.");
            }

            if (input.Amount < 0)
            {
                throw new UserFriendlyException("Amount cannot be negative.");
            }

            if (!Enum.IsDefined(typeof(BillingCycle), input.Cycle))
            {
                throw new UserFriendlyException("Choose monthly or yearly.");
            }

            var tenant = await EnsureTenantExists(input.TenantId);
            string planName;
            int? packageId = null;
            if (input.PackageId.HasValue)
            {
                var package = await _packages.FirstOrDefaultAsync(input.PackageId.Value);
                if (package == null)
                {
                    throw new UserFriendlyException("That plan was not found.");
                }

                planName = package.Name;
                packageId = package.Id;
            }
            else
            {
                throw new UserFriendlyException("Choose a plan.");
            }

            var entity = await _subscriptions.GetAll()
                .FirstOrDefaultAsync(x => x.TenantId == input.TenantId);
            if (entity == null)
            {
                entity = new TenantSubscription { TenantId = input.TenantId };
            }

            entity.PackageId = packageId;
            entity.PlanName = planName;
            entity.Cycle = input.Cycle;
            entity.Amount = decimal.Round(input.Amount, 2);
            entity.PaidUntil = input.ClearPaidUntil || !input.PaidUntil.HasValue
                ? (DateTime?)null
                : input.PaidUntil.Value.Date;

            if (entity.Id == 0)
            {
                await _subscriptions.InsertAsync(entity);
            }
            else
            {
                await _subscriptions.UpdateAsync(entity);
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            var pending = await HasPending(input.TenantId);
            return MapSubscription(entity, pending, new List<Tenant> { tenant });
        }

        [AbpAuthorize(PermissionNames.Pages_Subscriptions)]
        public virtual async Task<List<SubscriptionPaymentDto>> GetPendingPayments()
        {
            var rows = await _payments.GetAll()
                .Where(x => x.Status == SubscriptionPaymentStatus.Pending)
                .OrderBy(x => x.CreationTime)
                .ToListAsync();
            var tenants = await _tenants.GetAll().ToListAsync();
            return rows.Select(x => MapPayment(x, tenants)).ToList();
        }

        [AbpAuthorize(PermissionNames.Pages_Subscriptions)]
        public virtual async Task<ReceiptFileDto> GetReceipt(int id)
        {
            var payment = await _payments.FirstOrDefaultAsync(id);
            if (payment == null || payment.ReceiptImage == null || payment.ReceiptImage.Length == 0)
            {
                throw new UserFriendlyException("Receipt was not found.");
            }

            return new ReceiptFileDto
            {
                FileName = string.IsNullOrWhiteSpace(payment.ReceiptFileName) ? "receipt.jpg" : payment.ReceiptFileName,
                ContentType = string.IsNullOrWhiteSpace(payment.ReceiptContentType) ? "image/jpeg" : payment.ReceiptContentType,
                Base64 = Convert.ToBase64String(payment.ReceiptImage)
            };
        }

        [AbpAuthorize(PermissionNames.Pages_Subscriptions)]
        public virtual async Task<TenantSubscriptionDto> ApprovePayment(ReviewSubscriptionPaymentInput input)
        {
            var payment = await GetPendingPayment(input);
            return await ApplyApproval(payment, CleanNote(input?.Note));
        }

        [AbpAllowAnonymous]
        public virtual async Task<SubscriptionApprovalPageDto> ApproveFromEmail(string token)
        {
            var key = (token ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new UserFriendlyException("This approval link is not valid.");
            }

            var payment = await _payments.GetAll().FirstOrDefaultAsync(x => x.ApprovalToken == key);
            if (payment == null)
            {
                throw new UserFriendlyException("This approval link is not valid.");
            }

            var tenant = await _tenants.FirstOrDefaultAsync(payment.TenantId);
            if (payment.Status == SubscriptionPaymentStatus.Approved)
            {
                var current = await _subscriptions.GetAll().FirstOrDefaultAsync(x => x.TenantId == payment.TenantId);
                return new SubscriptionApprovalPageDto
                {
                    AlreadyApproved = true,
                    Message = "This payment was already approved.",
                    TenantName = tenant?.Name,
                    PlanName = payment.PlanName,
                    PaidUntil = current?.PaidUntil
                };
            }

            if (payment.Status != SubscriptionPaymentStatus.Pending)
            {
                throw new UserFriendlyException("This payment is no longer waiting for approval.");
            }

            var updated = await ApplyApproval(payment, "Approved from email");
            return new SubscriptionApprovalPageDto
            {
                Message = "Payment approved. The plan is active again.",
                TenantName = updated.TenantName ?? tenant?.Name,
                PlanName = updated.PlanName,
                PaidUntil = updated.PaidUntil
            };
        }

        [AbpAuthorize(PermissionNames.Pages_Subscriptions)]
        public virtual async Task RejectPayment(ReviewSubscriptionPaymentInput input)
        {
            var payment = await GetPendingPayment(input);
            var note = CleanNote(input?.Note);
            if (string.IsNullOrWhiteSpace(note))
            {
                throw new UserFriendlyException("Add a note so the business knows why this was rejected.");
            }

            payment.Status = SubscriptionPaymentStatus.Rejected;
            payment.ReviewNote = note;
            payment.ReviewedTime = Clock.Now;
            payment.ReviewedByUserId = AbpSession.UserId;
            await _payments.UpdateAsync(payment);
        }

        public virtual async Task<SubscriptionAccessDto> GetAccessStatus()
        {
            if (!AbpSession.TenantId.HasValue)
            {
                return new SubscriptionAccessDto();
            }

            var tenantId = AbpSession.TenantId.Value;
            var subscription = await _subscriptions.GetAll().FirstOrDefaultAsync(x => x.TenantId == tenantId);
            if (subscription?.PaidUntil == null)
            {
                return new SubscriptionAccessDto
                {
                    PlanName = subscription?.PlanName,
                    Amount = subscription?.Amount ?? 0
                };
            }

            var today = Clock.Now.Date;
            var expired = IsPaymentDatePassed(subscription.PaidUntil, today);
            return new SubscriptionAccessDto
            {
                Expired = expired,
                PlanName = subscription.PlanName,
                Amount = subscription.Amount,
                PaidUntil = subscription.PaidUntil,
                DaysLeft = expired ? 0 : (subscription.PaidUntil.Value.Date - today).Days,
                HasPendingPayment = await HasPending(tenantId)
            };
        }

        [AbpAuthorize(PermissionNames.Pages_Billing)]
        public virtual async Task<MySubscriptionDto> GetMine()
        {
            var tenantId = RequireTenant();
            var subscription = await _subscriptions.GetAll().FirstOrDefaultAsync(x => x.TenantId == tenantId);
            var pending = await HasPending(tenantId);
            var tenants = await _tenants.GetAll().Where(x => x.Id == tenantId).ToListAsync();
            var methods = await _paymentMethods.GetAll()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Kind)
                .ToListAsync();
            var payments = await _payments.GetAll()
                .Where(x => x.TenantId == tenantId)
                .OrderByDescending(x => x.CreationTime)
                .Take(20)
                .ToListAsync();

            var plans = await LoadAvailablePlans();
            return new MySubscriptionDto
            {
                Subscription = subscription == null
                    ? EmptySubscription(tenantId, tenants)
                    : MapSubscription(subscription, pending, tenants),
                PaymentMethods = methods.Select(x => MapMethod(x, includeQr: true)).ToList(),
                Payments = payments.Select(x => MapPayment(x, tenants)).ToList(),
                AvailablePlans = MergeCurrentPlanForRenewal(plans, subscription)
            };
        }

        [AbpAuthorize(PermissionNames.Pages_Billing)]
        public virtual async Task<SubscriptionPaymentDto> SubmitPayment(SubmitSubscriptionPaymentInput input)
        {
            var tenantId = RequireTenant();
            if (input == null || input.PaymentMethodId <= 0)
            {
                throw new UserFriendlyException("Choose how you paid.");
            }

            if (await HasPending(tenantId))
            {
                throw new UserFriendlyException("A receipt is already waiting for approval.");
            }

            var subscription = await _subscriptions.GetAll().FirstOrDefaultAsync(x => x.TenantId == tenantId);
            var resolved = await ResolvePurchase(input, subscription);

            var method = await _paymentMethods.GetAll()
                .FirstOrDefaultAsync(x => x.Id == input.PaymentMethodId && x.IsActive);
            if (method == null)
            {
                throw new UserFriendlyException("That payment method is not available.");
            }

            var image = DecodeImage(input.ReceiptBase64, input.ReceiptContentType, MaxReceiptBytes, "Receipt");
            var payment = new SubscriptionPayment
            {
                TenantId = tenantId,
                PackageId = resolved.PackageId,
                PlanName = resolved.PlanName,
                Cycle = resolved.Cycle,
                Amount = resolved.Amount,
                Method = method.Kind,
                PaymentMethodId = method.Id,
                Status = SubscriptionPaymentStatus.Pending,
                ReceiptImage = image.Bytes,
                ReceiptContentType = image.ContentType,
                ReceiptFileName = TrimFileName(input.ReceiptFileName),
                ApprovalToken = Guid.NewGuid().ToString("N")
            };
            await _payments.InsertAsync(payment);
            await CurrentUnitOfWork.SaveChangesAsync();

            var tenants = await _tenants.GetAll().Where(x => x.Id == tenantId).ToListAsync();
            await SendApprovalEmail(payment, tenants.FirstOrDefault(), method);
            return MapPayment(payment, tenants);
        }

        private async Task<List<AssignablePlanDto>> LoadAvailablePlans()
        {
            var rows = await _packages.GetAll()
                .Where(x => x.IsActive && !x.ContactViaWhatsApp && (x.MonthlyAmount > 0 || x.YearlyAmount > 0))
                .OrderBy(x => x.SortOrder)
                .Select(x => new AssignablePlanDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    MonthlyAmount = x.MonthlyAmount,
                    YearlyAmount = x.YearlyAmount
                })
                .ToListAsync();

            foreach (var row in rows)
            {
                FillMissingCycleAmounts(row);
            }

            return rows;
        }

        /// <summary>
        /// Clients can renew the plan the host already assigned, even when package monthly/yearly
        /// catalog prices are still zero. Missing yearly is offered as monthly x 12 (and the reverse).
        /// </summary>
        private static List<AssignablePlanDto> MergeCurrentPlanForRenewal(
            List<AssignablePlanDto> plans,
            TenantSubscription subscription)
        {
            var list = plans == null ? new List<AssignablePlanDto>() : plans.ToList();
            if (subscription == null ||
                subscription.Amount <= 0 ||
                string.IsNullOrWhiteSpace(subscription.PlanName))
            {
                return list;
            }

            AssignablePlanDto row = null;
            if (subscription.PackageId.HasValue && subscription.PackageId.Value > 0)
            {
                row = list.FirstOrDefault(x => x.Id == subscription.PackageId.Value);
            }

            if (row == null)
            {
                row = new AssignablePlanDto
                {
                    Id = subscription.PackageId.GetValueOrDefault(),
                    Name = subscription.PlanName,
                    MonthlyAmount = 0,
                    YearlyAmount = 0
                };
                list.Insert(0, row);
            }

            if (subscription.Cycle == BillingCycle.Yearly)
            {
                if (row.YearlyAmount <= 0)
                {
                    row.YearlyAmount = subscription.Amount;
                }
            }
            else if (row.MonthlyAmount <= 0)
            {
                row.MonthlyAmount = subscription.Amount;
            }

            FillMissingCycleAmounts(row);
            return list;
        }

        private static void FillMissingCycleAmounts(AssignablePlanDto row)
        {
            if (row == null)
            {
                return;
            }

            if (row.MonthlyAmount > 0 && row.YearlyAmount <= 0)
            {
                row.YearlyAmount = row.MonthlyAmount * 12;
            }
            else if (row.YearlyAmount > 0 && row.MonthlyAmount <= 0)
            {
                row.MonthlyAmount = Math.Round(row.YearlyAmount / 12m, 2, MidpointRounding.AwayFromZero);
            }
        }

        private static decimal AmountForCycle(decimal monthly, decimal yearly, BillingCycle cycle)
        {
            if (cycle == BillingCycle.Yearly)
            {
                if (yearly > 0)
                {
                    return yearly;
                }

                return monthly > 0 ? monthly * 12 : 0;
            }

            if (monthly > 0)
            {
                return monthly;
            }

            return yearly > 0
                ? Math.Round(yearly / 12m, 2, MidpointRounding.AwayFromZero)
                : 0;
        }

        private static decimal AmountFromSubscription(TenantSubscription subscription, BillingCycle cycle)
        {
            if (subscription == null || subscription.Amount <= 0)
            {
                return 0;
            }

            if (subscription.Cycle == cycle)
            {
                return subscription.Amount;
            }

            if (cycle == BillingCycle.Yearly && subscription.Cycle == BillingCycle.Monthly)
            {
                return subscription.Amount * 12;
            }

            if (cycle == BillingCycle.Monthly && subscription.Cycle == BillingCycle.Yearly)
            {
                return Math.Round(subscription.Amount / 12m, 2, MidpointRounding.AwayFromZero);
            }

            return subscription.Amount;
        }

        private async Task<(int? PackageId, string PlanName, BillingCycle Cycle, decimal Amount)> ResolvePurchase(
            SubmitSubscriptionPaymentInput input,
            TenantSubscription subscription)
        {
            // Renew / buy more of the assigned plan when catalog prices are missing.
            if (input.PackageId <= 0)
            {
                if (subscription == null ||
                    subscription.Amount <= 0 ||
                    string.IsNullOrWhiteSpace(subscription.PlanName))
                {
                    throw new UserFriendlyException("Choose a subscription plan.");
                }

                var renewAmount = AmountFromSubscription(subscription, input.Cycle);
                if (renewAmount <= 0)
                {
                    throw new UserFriendlyException("That billing cycle is not available for your plan.");
                }

                return (
                    subscription.PackageId,
                    subscription.PlanName,
                    input.Cycle,
                    renewAmount);
            }

            var package = await _packages.GetAll()
                .FirstOrDefaultAsync(x => x.Id == input.PackageId && x.IsActive);
            if (package == null || package.ContactViaWhatsApp)
            {
                // Still allow renewing the assigned package even if it was later deactivated.
                if (subscription != null &&
                    subscription.PackageId == input.PackageId &&
                    subscription.Amount > 0 &&
                    !string.IsNullOrWhiteSpace(subscription.PlanName))
                {
                    var fallback = AmountFromSubscription(subscription, input.Cycle);
                    if (fallback > 0)
                    {
                        return (
                            subscription.PackageId,
                            subscription.PlanName,
                            input.Cycle,
                            fallback);
                    }
                }

                throw new UserFriendlyException("That plan is not available.");
            }

            var amount = AmountForCycle(package.MonthlyAmount, package.YearlyAmount, input.Cycle);

            if (amount <= 0 &&
                subscription != null &&
                subscription.PackageId == package.Id &&
                subscription.Amount > 0)
            {
                amount = AmountFromSubscription(subscription, input.Cycle);
            }

            if (amount <= 0)
            {
                throw new UserFriendlyException(
                    input.Cycle == BillingCycle.Yearly
                        ? "Yearly billing is not available for that plan."
                        : "Monthly billing is not available for that plan.");
            }

            return (package.Id, package.Name, input.Cycle, amount);
        }

        private async Task<TenantSubscriptionDto> ApplyApproval(SubscriptionPayment payment, string note)
        {
            var subscription = await _subscriptions.GetAll()
                .FirstOrDefaultAsync(x => x.TenantId == payment.TenantId);
            if (subscription == null)
            {
                subscription = new TenantSubscription { TenantId = payment.TenantId };
            }

            var today = Clock.Now.Date;
            // Always add the purchased period onto remaining time (or today if expired / new).
            var stillActive = subscription.PaidUntil.HasValue && !IsPaymentDatePassed(subscription.PaidUntil, today);
            var start = stillActive ? subscription.PaidUntil.Value.Date : today;
            subscription.PaidUntil = payment.Cycle == BillingCycle.Yearly
                ? start.AddYears(1)
                : start.AddMonths(1);
            subscription.PackageId = payment.PackageId;
            subscription.PlanName = payment.PlanName;
            subscription.Cycle = payment.Cycle;
            subscription.Amount = payment.Amount;

            if (subscription.Id == 0)
            {
                await _subscriptions.InsertAsync(subscription);
            }
            else
            {
                await _subscriptions.UpdateAsync(subscription);
            }

            payment.Status = SubscriptionPaymentStatus.Approved;
            payment.ReviewNote = note;
            payment.ReviewedTime = Clock.Now;
            payment.ReviewedByUserId = AbpSession.UserId;
            await _payments.UpdateAsync(payment);
            await CurrentUnitOfWork.SaveChangesAsync();

            var tenant = await _tenants.FirstOrDefaultAsync(payment.TenantId);
            return MapSubscription(
                subscription,
                false,
                tenant == null ? new List<Tenant>() : new List<Tenant> { tenant });
        }

        private async Task SendApprovalEmail(SubscriptionPayment payment, Tenant tenant, PaymentMethod method)
        {
            try
            {
                var businessName = tenant?.Name ?? tenant?.TenancyName ?? "A business";
                var planName = payment.PlanName ?? "Plan";
                var business = WebUtility.HtmlEncode(businessName);
                var plan = WebUtility.HtmlEncode(planName);
                var cycle = payment.Cycle == BillingCycle.Yearly ? "per year" : "per month";
                var amount = payment.Amount.ToString("N2");
                var methodName = WebUtility.HtmlEncode(KindName(payment.Method));
                var root = (_configuration["App:ServerRootAddress"] ?? string.Empty).Trim().TrimEnd('/');
                var link = string.IsNullOrWhiteSpace(root)
                    ? null
                    : root + "/api/SubscriptionPaymentApproval/Approve?token=" + Uri.EscapeDataString(payment.ApprovalToken);
                var button = string.IsNullOrWhiteSpace(link)
                    ? "<p>Open Subscriptions → Waiting list in the host account to approve it.</p>"
                    : "<p style=\"margin:24px 0;\"><a href=\"" + link + "\" style=\"display:inline-block;background:#15803d;color:#ffffff;padding:12px 22px;border-radius:8px;text-decoration:none;font-weight:700;\">Approve payment</a></p>";

                var body =
                    "<div style=\"font-family:Segoe UI,Arial,sans-serif;color:#0f172a;line-height:1.5;\">" +
                    "<h2 style=\"margin:0 0 12px;\">Subscription payment waiting</h2>" +
                    "<p><strong>" + business + "</strong> submitted a receipt for <strong>" + plan + "</strong>.</p>" +
                    "<p>Amount: <strong>PKR " + amount + "</strong> " + cycle + "<br/>Method: " + methodName +
                    (string.IsNullOrWhiteSpace(method?.AccountNumber) ? "" : " · " + WebUtility.HtmlEncode(method.AccountNumber)) +
                    "</p>" +
                    button +
                    "<p style=\"color:#64748b;font-size:13px;\">Approving adds one month or one year onto the current paid-until date (or from today if it has already passed).</p>" +
                    "</div>";

                await _mailSender.SendAsync(
                    ApprovalEmail,
                    businessName + " paid " + planName,
                    body,
                    isBodyHtml: true);
            }
            catch (Exception ex)
            {
                Logger.Warn("Could not email the subscription approval link.", ex);
            }
        }

        private async Task<SubscriptionPayment> GetPendingPayment(ReviewSubscriptionPaymentInput input)
        {
            if (input == null || input.Id <= 0)
            {
                throw new UserFriendlyException("Choose a payment.");
            }

            var payment = await _payments.FirstOrDefaultAsync(input.Id);
            if (payment == null || payment.Status != SubscriptionPaymentStatus.Pending)
            {
                throw new UserFriendlyException("That payment is no longer waiting.");
            }

            return payment;
        }

        private int RequireTenant()
        {
            if (!AbpSession.TenantId.HasValue)
            {
                throw new UserFriendlyException("Billing is available inside a business account.");
            }

            return AbpSession.TenantId.Value;
        }

        private async Task<Tenant> EnsureTenantExists(int tenantId)
        {
            var tenant = await _tenants.FirstOrDefaultAsync(tenantId);
            if (tenant == null)
            {
                throw new UserFriendlyException("Business was not found.");
            }

            return tenant;
        }

        private async Task<bool> HasPending(int tenantId)
        {
            return await _payments.GetAll().AnyAsync(x =>
                x.TenantId == tenantId && x.Status == SubscriptionPaymentStatus.Pending);
        }

        private async Task<HashSet<int>> PendingTenantIds()
        {
            var ids = await _payments.GetAll()
                .Where(x => x.Status == SubscriptionPaymentStatus.Pending)
                .Select(x => x.TenantId)
                .Distinct()
                .ToListAsync();
            return new HashSet<int>(ids);
        }

        /// <summary>The due date itself is expired. A later date is still active.</summary>
        private static bool IsPaymentDatePassed(DateTime? paidUntil, DateTime today)
        {
            return paidUntil.HasValue && paidUntil.Value.Date <= today;
        }

        private static TenantSubscriptionDto EmptySubscription(int tenantId, List<Tenant> tenants)
        {
            var tenant = tenants.FirstOrDefault(x => x.Id == tenantId);
            return new TenantSubscriptionDto
            {
                TenantId = tenantId,
                TenancyName = tenant?.TenancyName,
                TenantName = tenant?.Name,
                Cycle = BillingCycle.Monthly,
                CycleName = "per month",
                Status = "Unassigned",
                DaysLeft = 0,
                CanPay = false
            };
        }

        private static TenantSubscriptionDto MapSubscription(
            TenantSubscription row,
            bool hasPending,
            List<Tenant> tenants)
        {
            var today = Clock.Now.Date;
            var stillActive = row.PaidUntil.HasValue && !IsPaymentDatePassed(row.PaidUntil, today);
            var daysLeft = stillActive ? (row.PaidUntil.Value.Date - today).Days : 0;
            string status;
            if (hasPending)
            {
                status = "Waiting";
            }
            else if (!stillActive)
            {
                status = "Expired";
            }
            else if (daysLeft <= RenewalWindowDays)
            {
                status = "Expiring";
            }
            else
            {
                status = "Paid";
            }

            var tenant = tenants.FirstOrDefault(x => x.Id == row.TenantId);
            return new TenantSubscriptionDto
            {
                Id = row.Id,
                TenantId = row.TenantId,
                TenancyName = tenant?.TenancyName,
                TenantName = tenant?.Name,
                PackageId = row.PackageId,
                PlanName = row.PlanName,
                Cycle = row.Cycle,
                CycleName = row.Cycle == BillingCycle.Yearly ? "per year" : "per month",
                Amount = row.Amount,
                PaidUntil = row.PaidUntil,
                DaysLeft = daysLeft,
                Status = status,
                CanPay = !hasPending && row.Amount > 0 && (!stillActive || daysLeft <= RenewalWindowDays),
                HasPendingPayment = hasPending
            };
        }

        private static PaymentMethodDto MapMethod(PaymentMethod row, bool includeQr)
        {
            var hasQr = row.QrImage != null && row.QrImage.Length > 0;
            return new PaymentMethodDto
            {
                Id = row.Id,
                Kind = row.Kind,
                KindName = KindName(row.Kind),
                AccountTitle = row.AccountTitle,
                AccountNumber = row.AccountNumber,
                BankName = row.BankName,
                Instructions = row.Instructions,
                IsActive = row.IsActive,
                HasQr = hasQr,
                QrBase64 = includeQr && hasQr ? Convert.ToBase64String(row.QrImage) : null,
                QrContentType = includeQr && hasQr ? row.QrContentType : null
            };
        }

        private static SubscriptionPaymentDto MapPayment(SubscriptionPayment row, List<Tenant> tenants)
        {
            var tenant = tenants.FirstOrDefault(x => x.Id == row.TenantId);
            return new SubscriptionPaymentDto
            {
                Id = row.Id,
                TenantId = row.TenantId,
                TenancyName = tenant?.TenancyName,
                TenantName = tenant?.Name,
                PlanName = row.PlanName,
                Cycle = row.Cycle,
                CycleName = row.Cycle == BillingCycle.Yearly ? "per year" : "per month",
                Amount = row.Amount,
                Method = row.Method,
                MethodName = KindName(row.Method),
                Status = row.Status,
                StatusName = row.Status.ToString(),
                ReviewNote = row.ReviewNote,
                CreationTime = row.CreationTime,
                ReviewedTime = row.ReviewedTime,
                HasReceipt = row.ReceiptImage != null && row.ReceiptImage.Length > 0
            };
        }

        private static string KindName(PaymentMethodKind kind)
        {
            switch (kind)
            {
                case PaymentMethodKind.Easypaisa:
                    return "Easypaisa";
                case PaymentMethodKind.JazzCash:
                    return "JazzCash";
                case PaymentMethodKind.Nayapay:
                    return "Nayapay";
                default:
                    return "Bank account";
            }
        }

        private static string CleanNote(string note)
        {
            return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }

        private static string TrimFileName(string fileName)
        {
            var name = string.IsNullOrWhiteSpace(fileName) ? "receipt.jpg" : fileName.Trim();
            return name.Length <= SubscriptionPayment.MaxFileNameLength
                ? name
                : name.Substring(0, SubscriptionPayment.MaxFileNameLength);
        }

        private static (byte[] Bytes, string ContentType) DecodeImage(
            string base64,
            string contentType,
            int maxBytes,
            string label)
        {
            var payload = (base64 ?? string.Empty).Trim();
            var comma = payload.IndexOf(',');
            if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
            {
                payload = payload.Substring(comma + 1);
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(payload);
            }
            catch (FormatException)
            {
                throw new UserFriendlyException(label + " is not a valid image.");
            }

            if (bytes.Length == 0 || bytes.Length > maxBytes)
            {
                throw new UserFriendlyException(label + " must be a PNG or JPG under " + (maxBytes / (1024 * 1024)) + " MB.");
            }

            var type = (contentType ?? string.Empty).Trim().ToLowerInvariant();
            if (type == "image/jpg")
            {
                type = "image/jpeg";
            }

            if (type != "image/png" && type != "image/jpeg" && type != "image/webp")
            {
                throw new UserFriendlyException(label + " must be a PNG, JPG, or WEBP image.");
            }

            return (bytes, type);
        }
    }
}
