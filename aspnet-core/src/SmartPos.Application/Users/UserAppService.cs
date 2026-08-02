using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.IdentityFramework;
using Abp.Linq.Extensions;
using Abp.Localization;
using Abp.Runtime.Session;
using Abp.UI;
using SmartPos.Authorization;
using SmartPos.Authorization.Accounts;
using SmartPos.Authorization.Roles;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Roles.Dto;
using SmartPos.Users.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SmartPos.Users
{
    [AbpAuthorize(PermissionNames.Pages_Users)]
    public class UserAppService : AsyncCrudAppService<User, UserDto, long, PagedUserResultRequestDto, CreateUserDto, UserDto>, IUserAppService
    {
        private readonly UserManager _userManager;
        private readonly RoleManager _roleManager;
        private readonly IRepository<Role> _roleRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IAbpSession _abpSession;
        private readonly LogInManager _logInManager;
        private readonly IRepository<UserBranch> _userBranchRepository;
        private readonly IRepository<Branch> _branchRepository;

        public UserAppService(
            IRepository<User, long> repository,
            UserManager userManager,
            RoleManager roleManager,
            IRepository<Role> roleRepository,
            IPasswordHasher<User> passwordHasher,
            IAbpSession abpSession,
            LogInManager logInManager,
            IRepository<UserBranch> userBranchRepository,
            IRepository<Branch> branchRepository)
            : base(repository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
            _abpSession = abpSession;
            _logInManager = logInManager;
            _userBranchRepository = userBranchRepository;
            _branchRepository = branchRepository;
        }

        public override async Task<UserDto> CreateAsync(CreateUserDto input)
        {
            CheckCreatePermission();

            var user = ObjectMapper.Map<User>(input);

            user.TenantId = AbpSession.TenantId;
            user.IsEmailConfirmed = true;
            user.UserImageUrl = UserImageStore.SaveBase64Image(input.ImageBase64);

            await _userManager.InitializeOptionsAsync(AbpSession.TenantId);

            CheckErrors(await _userManager.CreateAsync(user, input.Password));

            if (input.RoleNames != null)
            {
                CheckErrors(await _userManager.SetRolesAsync(user, input.RoleNames));
            }

            CurrentUnitOfWork.SaveChanges();

            await SyncUserBranchesAsync(user.Id, input.BranchIds);

            return await GetAsync(new EntityDto<long>(user.Id));
        }

        public override async Task<UserDto> UpdateAsync(UserDto input)
        {
            CheckUpdatePermission();

            var user = await _userManager.GetUserByIdAsync(input.Id);

            MapToEntity(input, user);

            if (UserImageStore.IsNewImagePayload(input.ImageBase64))
            {
                UserImageStore.DeleteIfExists(user.UserImageUrl);
                user.UserImageUrl = UserImageStore.SaveBase64Image(input.ImageBase64);
            }

            CheckErrors(await _userManager.UpdateAsync(user));

            if (input.RoleNames != null)
            {
                CheckErrors(await _userManager.SetRolesAsync(user, input.RoleNames));
            }

            await SyncUserBranchesAsync(user.Id, input.BranchIds);

            return await GetAsync(input);
        }

        public override async Task DeleteAsync(EntityDto<long> input)
        {
            var user = await _userManager.GetUserByIdAsync(input.Id);
            UserImageStore.DeleteIfExists(user.UserImageUrl);
            await _userManager.DeleteAsync(user);
        }

        [AbpAuthorize(PermissionNames.Pages_Users_Activation)]
        public async Task Activate(EntityDto<long> user)
        {
            await Repository.UpdateAsync(user.Id, async (entity) =>
            {
                entity.IsActive = true;
            });
        }

        [AbpAuthorize(PermissionNames.Pages_Users_Activation)]
        public async Task DeActivate(EntityDto<long> user)
        {
            await Repository.UpdateAsync(user.Id, async (entity) =>
            {
                entity.IsActive = false;
            });
        }

        public async Task<ListResultDto<RoleDto>> GetRoles()
        {
            var roles = await _roleRepository.GetAllListAsync();
            return new ListResultDto<RoleDto>(ObjectMapper.Map<List<RoleDto>>(roles));
        }

        public async Task<ListResultDto<string>> GetUserPermissions(EntityDto<long> input)
        {
            var user = await _userManager.GetUserByIdAsync(input.Id);
            var grantedPermissions = await _userManager.GetGrantedPermissionsAsync(user);

            return new ListResultDto<string>(
                grantedPermissions.Select(p => p.Name).ToList()
            );
        }

        public async Task UpdateUserPermissions(UpdateUserPermissionsDto input)
        {
            var user = await _userManager.GetUserByIdAsync(input.Id);
            var permissionNames = (input.GrantedPermissionNames ?? new List<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToList();

            // Clear user-level grants/prohibits so role permissions stay effective.
            // Then only GRANT extra permissions selected on the user (additive).
            // Do NOT use SetGrantedPermissionsAsync here — it prohibits role permissions
            // that are missing from the checkbox list and breaks POS/Sales access.
            await _userManager.ResetAllPermissionsAsync(user);

            foreach (var permissionName in permissionNames)
            {
                var permission = PermissionManager.GetPermissionOrNull(permissionName);
                if (permission == null)
                {
                    continue;
                }

                await _userManager.GrantPermissionAsync(user, permission);
            }
        }

        public async Task ChangeLanguage(ChangeUserLanguageDto input)
        {
            await SettingManager.ChangeSettingForUserAsync(
                AbpSession.ToUserIdentifier(),
                LocalizationSettingNames.DefaultLanguage,
                input.LanguageName
            );
        }

        protected override User MapToEntity(CreateUserDto createInput)
        {
            var user = ObjectMapper.Map<User>(createInput);
            user.SetNormalizedNames();
            return user;
        }

        protected override void MapToEntity(UserDto input, User user)
        {
            ObjectMapper.Map(input, user);
            user.SetNormalizedNames();
        }

        public override async Task<UserDto> GetAsync(EntityDto<long> input)
        {
            var dto = await base.GetAsync(input);
            dto.BranchIds = await GetBranchIdsAsync(input.Id);
            return dto;
        }

        public override async Task<PagedResultDto<UserDto>> GetAllAsync(PagedUserResultRequestDto input)
        {
            var result = await base.GetAllAsync(input);
            await PopulateBranchIdsAsync(result.Items);
            return result;
        }

        protected override UserDto MapToEntityDto(User user)
        {
            var roleIds = user.Roles.Select(x => x.RoleId).ToArray();

            var roles = _roleManager.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.NormalizedName);

            var userDto = base.MapToEntityDto(user);
            userDto.RoleNames = roles.ToArray();

            return userDto;
        }

        protected override IQueryable<User> CreateFilteredQuery(PagedUserResultRequestDto input)
        {
            return Repository.GetAllIncluding(x => x.Roles)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x => x.UserName.Contains(input.Keyword) || x.Name.Contains(input.Keyword) || x.EmailAddress.Contains(input.Keyword))
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);
        }

        protected override async Task<User> GetEntityByIdAsync(long id)
        {
            var user = await Repository.GetAllIncluding(x => x.Roles).FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                throw new EntityNotFoundException(typeof(User), id);
            }

            return user;
        }

        protected override IQueryable<User> ApplySorting(IQueryable<User> query, PagedUserResultRequestDto input)
        {
            return query.OrderBy(r => r.UserName);
        }

        protected virtual void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }

        private async Task<int[]> GetBranchIdsAsync(long userId)
        {
            return await _userBranchRepository.GetAll()
                .Where(x => x.UserId == userId)
                .Select(x => x.BranchId)
                .ToArrayAsync();
        }

        private async Task PopulateBranchIdsAsync(IReadOnlyList<UserDto> users)
        {
            if (users == null || users.Count == 0)
            {
                return;
            }

            var userIds = users.Select(x => x.Id).ToList();
            var assignments = await _userBranchRepository.GetAll()
                .Where(x => userIds.Contains(x.UserId))
                .Select(x => new { x.UserId, x.BranchId })
                .ToListAsync();

            var byUserId = assignments
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.BranchId).ToArray());

            foreach (var user in users)
            {
                user.BranchIds = byUserId.TryGetValue(user.Id, out var branchIds)
                    ? branchIds
                    : Array.Empty<int>();
            }
        }

        private async Task SyncUserBranchesAsync(long userId, int[] branchIds)
        {
            branchIds = branchIds ?? Array.Empty<int>();
            var distinctBranchIds = branchIds.Where(id => id > 0).Distinct().ToArray();

            if (distinctBranchIds.Length > 0)
            {
                var validCount = await _branchRepository.GetAll()
                    .CountAsync(x => distinctBranchIds.Contains(x.Id));
                if (validCount != distinctBranchIds.Length)
                {
                    throw new UserFriendlyException("One or more selected branches were not found for this tenant.");
                }
            }

            var existing = await _userBranchRepository.GetAll()
                .Where(x => x.UserId == userId)
                .ToListAsync();

            var existingIds = existing.Select(x => x.BranchId).ToHashSet();
            foreach (var assignment in existing.Where(x => !distinctBranchIds.Contains(x.BranchId)).ToList())
            {
                await _userBranchRepository.DeleteAsync(assignment);
            }

            foreach (var branchId in distinctBranchIds.Where(id => !existingIds.Contains(id)))
            {
                await _userBranchRepository.InsertAsync(new UserBranch
                {
                    TenantId = AbpSession.TenantId,
                    UserId = userId,
                    BranchId = branchId
                });
            }
        }

        public async Task<bool> ChangePassword(ChangePasswordDto input)
        {
            await _userManager.InitializeOptionsAsync(AbpSession.TenantId);

            var user = await _userManager.FindByIdAsync(AbpSession.GetUserId().ToString());
            if (user == null)
            {
                throw new Exception("There is no current user!");
            }
            
            if (await _userManager.CheckPasswordAsync(user, input.CurrentPassword))
            {
                CheckErrors(await _userManager.ChangePasswordAsync(user, input.NewPassword));
            }
            else
            {
                CheckErrors(IdentityResult.Failed(new IdentityError
                {
                    Description = "Incorrect password."
                }));
            }

            return true;
        }

        public async Task<bool> ResetPassword(ResetPasswordDto input)
        {
            if (_abpSession.UserId == null)
            {
                throw new UserFriendlyException("Please log in before attempting to reset password.");
            }
            
            var currentUser = await _userManager.GetUserByIdAsync(_abpSession.GetUserId());
            var loginAsync = await _logInManager.LoginAsync(currentUser.UserName, input.AdminPassword, shouldLockout: false);
            if (loginAsync.Result != AbpLoginResultType.Success)
            {
                throw new UserFriendlyException("Your 'Admin Password' did not match the one on record.  Please try again.");
            }
            
            if (currentUser.IsDeleted || !currentUser.IsActive)
            {
                return false;
            }
            
            var roles = await _userManager.GetRolesAsync(currentUser);
            if (!roles.Contains(StaticRoleNames.Tenants.Admin))
            {
                throw new UserFriendlyException("Only administrators may reset passwords.");
            }

            var user = await _userManager.GetUserByIdAsync(input.UserId);
            if (user != null)
            {
                user.Password = _passwordHasher.HashPassword(user, input.NewPassword);
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return true;
        }
    }
}

