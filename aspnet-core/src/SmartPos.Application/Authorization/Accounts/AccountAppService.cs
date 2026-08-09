using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.IdentityFramework;
using Abp.MultiTenancy;
using Abp.UI;
using Abp.Zero.Configuration;
using Microsoft.AspNetCore.Identity;
using SmartPos.Authorization.Accounts.Dto;
using SmartPos.Authorization.Roles;
using SmartPos.Authorization.Users;
using SmartPos.Editions;
using SmartPos.Lookups;
using SmartPos.MultiTenancy;

namespace SmartPos.Authorization.Accounts
{
    public class AccountAppService : SmartPosAppServiceBase, IAccountAppService
    {
        // from: http://regexlib.com/REDetails.aspx?regexp_id=1923
        public const string PasswordRegex = "(?=^.{8,}$)(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?!.*\\s)[0-9a-zA-Z!@#$%^&*()]*$";

        private readonly UserRegistrationManager _userRegistrationManager;
        private readonly TenantManager _tenantManager;
        private readonly EditionManager _editionManager;
        private readonly UserManager _userManager;
        private readonly RoleManager _roleManager;
        private readonly IRepository<LookUp> _lookUpRepository;
        private readonly IAbpZeroDbMigrator _abpZeroDbMigrator;

        public AccountAppService(
            UserRegistrationManager userRegistrationManager,
            TenantManager tenantManager,
            EditionManager editionManager,
            UserManager userManager,
            RoleManager roleManager,
            IRepository<LookUp> lookUpRepository,
            IAbpZeroDbMigrator abpZeroDbMigrator)
        {
            _userRegistrationManager = userRegistrationManager;
            _tenantManager = tenantManager;
            _editionManager = editionManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _lookUpRepository = lookUpRepository;
            _abpZeroDbMigrator = abpZeroDbMigrator;
        }

        public async Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input)
        {
            var tenant = await TenantManager.FindByTenancyNameAsync(input.TenancyName);
            if (tenant == null)
            {
                return new IsTenantAvailableOutput(TenantAvailabilityState.NotFound);
            }

            if (!tenant.IsActive)
            {
                return new IsTenantAvailableOutput(TenantAvailabilityState.InActive);
            }

            return new IsTenantAvailableOutput(TenantAvailabilityState.Available, tenant.Id);
        }

        public async Task<RegisterOutput> Register(RegisterInput input)
        {
            var user = await _userRegistrationManager.RegisterAsync(
                input.Name,
                input.Surname,
                input.EmailAddress,
                input.UserName,
                input.Password,
                true // Assumed email address is always confirmed. Change this if you want to implement email confirmation.
            );

            var isEmailConfirmationRequiredForLogin = await SettingManager.GetSettingValueAsync<bool>(AbpZeroSettingNames.UserManagement.IsEmailConfirmationRequiredForLogin);

            return new RegisterOutput
            {
                CanLogin = user.IsActive && (user.IsEmailConfirmed || !isEmailConfirmationRequiredForLogin)
            };
        }

        /// <summary>
        /// Public self-service signup: creates tenant + admin user only.
        /// The admin creates their first branch after login.
        /// </summary>
        [AbpAllowAnonymous]
        public async Task<SignUpTenantOutput> SignUpTenant(SignUpTenantInput input)
        {
            NormalizeSignUpInput(input);
            ValidateSignUpInput(input);

            var existing = await _tenantManager.FindByTenancyNameAsync(input.TenancyName);
            if (existing != null)
            {
                throw new UserFriendlyException(
                    $"Tenancy name \"{input.TenancyName}\" is already taken. Please choose another.");
            }

            var tenant = new Tenant(input.TenancyName, input.Name)
            {
                IsActive = true
            };

            var defaultEdition = await _editionManager.FindByNameAsync(EditionManager.DefaultEditionName);
            if (defaultEdition != null)
            {
                tenant.EditionId = defaultEdition.Id;
            }

            await _tenantManager.CreateAsync(tenant);
            await CurrentUnitOfWork.SaveChangesAsync();

            _abpZeroDbMigrator.CreateOrMigrateForTenant(tenant);

            using (CurrentUnitOfWork.SetTenantId(tenant.Id))
            {
                CheckErrors(await _roleManager.CreateStaticRoles(tenant.Id));
                await CurrentUnitOfWork.SaveChangesAsync();

                var adminRole = _roleManager.Roles.Single(r => r.Name == StaticRoleNames.Tenants.Admin);
                await _roleManager.GrantAllPermissionsAsync(adminRole);

                var adminUser = new User
                {
                    TenantId = tenant.Id,
                    UserName = input.AdminUserName,
                    Name = input.AdminName,
                    Surname = input.AdminSurname,
                    EmailAddress = input.AdminEmailAddress,
                    IsActive = true,
                    IsEmailConfirmed = true,
                    BranchId = null,
                    Roles = new List<UserRole>()
                };
                adminUser.SetNormalizedNames();

                await _userManager.InitializeOptionsAsync(tenant.Id);
                CheckErrors(await _userManager.CreateAsync(adminUser, input.AdminPassword));
                await CurrentUnitOfWork.SaveChangesAsync();

                CheckErrors(await _userManager.AddToRoleAsync(adminUser, adminRole.Name));
                await CurrentUnitOfWork.SaveChangesAsync();

                await SeedDefaultLookUpsAsync(tenant.Id);
            }

            return new SignUpTenantOutput
            {
                TenantId = tenant.Id,
                TenancyName = tenant.TenancyName,
                Name = tenant.Name,
                AdminUserName = input.AdminUserName,
                CanLogin = true
            };
        }

        private async Task SeedDefaultLookUpsAsync(int tenantId)
        {
            foreach (var item in LookUpSeedData.Items)
            {
                // BranchStatus is host-scoped (Branch.StatusId FK).
                if (item.Type == LookUpTypes.BranchStatus)
                {
                    continue;
                }

                await _lookUpRepository.InsertAsync(new LookUp
                {
                    TenantId = tenantId,
                    Type = item.Type,
                    Name = item.Name,
                    DisplayName = item.DisplayName,
                    SortOrder = item.SortOrder,
                    IsActive = true
                });
            }

            await CurrentUnitOfWork.SaveChangesAsync();
        }

        private static void NormalizeSignUpInput(SignUpTenantInput input)
        {
            input.TenancyName = input.TenancyName?.Trim().ToLowerInvariant();
            input.Name = input.Name?.Trim();
            input.AdminName = input.AdminName?.Trim();
            input.AdminSurname = input.AdminSurname?.Trim();
            input.AdminEmailAddress = input.AdminEmailAddress?.Trim();
            input.AdminUserName = input.AdminUserName?.Trim();
        }

        private static void ValidateSignUpInput(SignUpTenantInput input)
        {
            if (string.IsNullOrWhiteSpace(input.TenancyName) ||
                string.IsNullOrWhiteSpace(input.Name) ||
                string.IsNullOrWhiteSpace(input.AdminName) ||
                string.IsNullOrWhiteSpace(input.AdminSurname) ||
                string.IsNullOrWhiteSpace(input.AdminEmailAddress) ||
                string.IsNullOrWhiteSpace(input.AdminUserName) ||
                string.IsNullOrWhiteSpace(input.AdminPassword))
            {
                throw new UserFriendlyException("All signup fields are required.");
            }

            if (input.AdminPassword.Length < 6)
            {
                throw new UserFriendlyException("Password must be at least 6 characters.");
            }
        }

        private void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}
