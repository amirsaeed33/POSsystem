using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using SmartPos.Authorization;
using SmartPos.Branches;
using SmartPos.BranchNotificationSettings.Dto;

namespace SmartPos.BranchNotificationSettings
{
    [AbpAuthorize(PermissionNames.Pages_NotificationSettings)]
    public class BranchNotificationSettingAppService : SmartPosAppServiceBase, IBranchNotificationSettingAppService
    {
        private readonly IRepository<BranchNotificationSetting> _settingRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;

        public BranchNotificationSettingAppService(
            IRepository<BranchNotificationSetting> settingRepository,
            IBranchAccessChecker branchAccessChecker)
        {
            _settingRepository = settingRepository;
            _branchAccessChecker = branchAccessChecker;
        }

        public async Task<BranchNotificationSettingDto> GetAsync(int branchId)
        {
            await _branchAccessChecker.EnsureCanAccessBranchAsync(branchId);

            var setting = await _settingRepository.FirstOrDefaultAsync(
                s => s.BranchId == branchId && s.TenantId == AbpSession.TenantId);

            if (setting == null)
            {
                return new BranchNotificationSettingDto
                {
                    Id = 0,
                    BranchId = branchId,
                    TenantId = AbpSession.TenantId,
                    IsEnabled = false,
                    NotifyOnSale = false,
                    NotifyOnPurchase = false,
                    NotifyOnExpense = false,
                    Emails = string.Empty,
                    EmailList = new List<string>()
                };
            }

            return MapToDto(setting);
        }

        public async Task<BranchNotificationSettingDto> CreateOrUpdateAsync(CreateOrUpdateBranchNotificationSettingDto input)
        {
            if (input == null || input.BranchId <= 0)
            {
                throw new UserFriendlyException("Invalid branch specified.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(input.BranchId);

            var cleanedEmails = CleanAndValidateEmails(input.Emails, input.EmailList);

            var setting = await _settingRepository.FirstOrDefaultAsync(
                s => s.BranchId == input.BranchId && s.TenantId == AbpSession.TenantId);

            if (setting == null)
            {
                setting = new BranchNotificationSetting
                {
                    TenantId = AbpSession.TenantId,
                    BranchId = input.BranchId,
                    IsEnabled = input.IsEnabled,
                    NotifyOnSale = input.NotifyOnSale,
                    NotifyOnPurchase = input.NotifyOnPurchase,
                    NotifyOnExpense = input.NotifyOnExpense,
                    Emails = cleanedEmails
                };

                await _settingRepository.InsertAsync(setting);
            }
            else
            {
                setting.IsEnabled = input.IsEnabled;
                setting.NotifyOnSale = input.NotifyOnSale;
                setting.NotifyOnPurchase = input.NotifyOnPurchase;
                setting.NotifyOnExpense = input.NotifyOnExpense;
                setting.Emails = cleanedEmails;

                await _settingRepository.UpdateAsync(setting);
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            return MapToDto(setting);
        }

        public async Task DeleteAsync(int branchId)
        {
            await _branchAccessChecker.EnsureCanAccessBranchAsync(branchId);

            var setting = await _settingRepository.FirstOrDefaultAsync(
                s => s.BranchId == branchId && s.TenantId == AbpSession.TenantId);

            if (setting != null)
            {
                await _settingRepository.DeleteAsync(setting);
                await CurrentUnitOfWork.SaveChangesAsync();
            }
        }

        private static string CleanAndValidateEmails(string rawEmails, List<string> emailList)
        {
            var rawList = new List<string>();

            if (!string.IsNullOrWhiteSpace(rawEmails))
            {
                rawList.AddRange(rawEmails.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            }

            if (emailList != null && emailList.Any())
            {
                rawList.AddRange(emailList);
            }

            var trimmedList = rawList
                .Select(e => e?.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();

            var emailChecker = new EmailAddressAttribute();
            var validEmails = new List<string>();

            foreach (var email in trimmedList)
            {
                if (!emailChecker.IsValid(email))
                {
                    throw new UserFriendlyException($"'{email}' is not a valid email address.");
                }

                validEmails.Add(email);
            }

            var distinctEmails = validEmails
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var joined = string.Join(", ", distinctEmails);

            if (joined.Length > BranchNotificationSetting.MaxEmailsLength)
            {
                throw new UserFriendlyException($"Recipient emails list exceeds maximum length of {BranchNotificationSetting.MaxEmailsLength} characters.");
            }

            return joined;
        }

        private static BranchNotificationSettingDto MapToDto(BranchNotificationSetting entity)
        {
            var emailList = string.IsNullOrWhiteSpace(entity.Emails)
                ? new List<string>()
                : entity.Emails.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            return new BranchNotificationSettingDto
            {
                Id = entity.Id,
                TenantId = entity.TenantId,
                BranchId = entity.BranchId,
                IsEnabled = entity.IsEnabled,
                NotifyOnSale = entity.NotifyOnSale,
                NotifyOnPurchase = entity.NotifyOnPurchase,
                NotifyOnExpense = entity.NotifyOnExpense,
                Emails = entity.Emails ?? string.Empty,
                EmailList = emailList
            };
        }
    }
}
