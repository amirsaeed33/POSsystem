using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.IdentityFramework;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Roles;
using SmartPos.Branches;
using SmartPos.Authorization.Users;
using SmartPos.Staffs.Dto;
using RoleManager = SmartPos.Authorization.Roles.RoleManager;

namespace SmartPos.Staffs
{
    [AbpAuthorize(PermissionNames.Pages_Staff)]
    public class StaffAppService : AsyncCrudAppService<Staff, StaffDto, int, PagedStaffResultRequestDto, CreateStaffDto, StaffDto>, IStaffAppService
    {
        private readonly StaffHistoryWriter _staffHistoryWriter;
        private readonly UserManager _userManager;
        private readonly RoleManager _roleManager;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IBranchContext _branchContext;
        private readonly IRepository<User, long> _userRepository;
        private readonly IRepository<Branch> _branchRepository;

        public StaffAppService(
            IRepository<Staff> repository,
            StaffHistoryWriter staffHistoryWriter,
            UserManager userManager,
            RoleManager roleManager,
            IPasswordHasher<User> passwordHasher,
            IBranchContext branchContext,
            IRepository<User, long> userRepository,
            IRepository<Branch> branchRepository)
            : base(repository)
        {
            _staffHistoryWriter = staffHistoryWriter;
            _userManager = userManager;
            _roleManager = roleManager;
            _passwordHasher = passwordHasher;
            _branchContext = branchContext;
            _userRepository = userRepository;
            _branchRepository = branchRepository;
        }

        public override async Task<StaffDto> CreateAsync(CreateStaffDto input)
        {
            CheckCreatePermission();

            var entity = MapToEntity(input);
            if (!entity.TenantId.HasValue)
            {
                entity.TenantId = AbpSession.TenantId;
            }

            var activeBranchId = ResolveBranchId();
            if (activeBranchId.HasValue)
            {
                entity.BranchId = activeBranchId.Value;
            }

            await Repository.InsertAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();

            await _staffHistoryWriter.WriteAsync(
                entity.Id,
                entity.BranchId,
                StaffHistoryAction.Created,
                $"Staff '{entity.Name}' created.");

            return await GetAsync(new EntityDto<int>(entity.Id));
        }

        public override async Task<StaffDto> UpdateAsync(StaffDto input)
        {
            CheckUpdatePermission();

            var entity = await GetEntityByIdAsync(input.Id);
            var previousDesignation = entity.Designation;
            var previousSalary = entity.BasicSalary;
            var previousName = entity.Name;
            var userId = entity.UserId;
            var loginPassword = entity.LoginPassword;

            MapToEntity(input, entity);
            entity.UserId = userId;
            entity.LoginPassword = loginPassword;

            await Repository.UpdateAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();

            await _staffHistoryWriter.WriteAsync(
                entity.Id,
                entity.BranchId,
                StaffHistoryAction.Updated,
                $"Staff '{previousName}' updated.");

            if (!string.Equals(previousDesignation ?? string.Empty, entity.Designation ?? string.Empty))
            {
                await _staffHistoryWriter.WriteAsync(
                    entity.Id,
                    entity.BranchId,
                    StaffHistoryAction.DesignationChanged,
                    $"Designation '{previousDesignation}' → '{entity.Designation}'.");
            }

            if (previousSalary != entity.BasicSalary)
            {
                await _staffHistoryWriter.WriteAsync(
                    entity.Id,
                    entity.BranchId,
                    StaffHistoryAction.SalaryChanged,
                    $"BasicSalary {previousSalary} → {entity.BasicSalary}.");
            }

            return await GetAsync(new EntityDto<int>(entity.Id));
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var entity = await Repository.GetAsync(input.Id);

            await _staffHistoryWriter.WriteAsync(
                entity.Id,
                entity.BranchId,
                StaffHistoryAction.Deleted,
                $"Staff '{entity.Name}' deleted.");

            await Repository.DeleteAsync(entity);
        }

        public async Task<StaffDto> CreateLoginAsync(CreateStaffLoginDto input)
        {
            CheckUpdatePermission();

            var staff = await Repository.GetAsync(input.StaffId);
            if (staff.UserId.HasValue)
            {
                throw new UserFriendlyException($"A login account already exists for {input.Email} staff member.");
            }

            if (!staff.BranchId.HasValue)
            {
                throw new UserFriendlyException("Staff must be assigned to a location before creating a login.");
            }

            var email = (input.Email ?? string.Empty).Trim();
            if (email.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("Email is required.");
            }

            var password = input.Password ?? string.Empty;
            if (password.Length < 6)
            {
                throw new UserFriendlyException("Password must be at least 6 characters.");
            }

            var existing = await _userManager.FindByNameAsync(email)
                           ?? await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                throw new UserFriendlyException($"A login account already exists for {input.Email} staff member.");
            }

            var nameParts = (staff.Name ?? string.Empty).Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : "Staff";
            var surname = nameParts.Length > 1 ? nameParts[1] : firstName;

            var user = new User
            {
                TenantId = AbpSession.TenantId,
                UserName = email,
                Name = firstName,
                Surname = surname,
                EmailAddress = email,
                IsEmailConfirmed = true,
                IsActive = staff.IsActive,
                BranchId = staff.BranchId
            };
            user.SetNormalizedNames();

            await _userManager.InitializeOptionsAsync(AbpSession.TenantId);
            CheckErrors(await _userManager.CreateAsync(user, password));

            var isRider = staff.Designation != null &&
                (staff.Designation.ToLower().Contains("rider") || staff.Designation.ToLower().Contains("delivery"));

            var isWaiter = staff.Designation != null &&
                (staff.Designation.ToLower().Contains("waiter") || staff.Designation.ToLower().Contains("server"));

            if (staff.BranchId.HasValue)
            {
                var branch = await _branchRepository.FirstOrDefaultAsync(staff.BranchId.Value);
                if (branch != null && branch.BusinessType == BusinessType.Restaurant)
                {
                    if (isRider)
                    {
                        var roleName = StaticRoleNames.Tenants.DeliveryRider;
                        var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.TenantId == user.TenantId && r.Name == roleName);
                        if (role == null)
                        {
                            role = new SmartPos.Authorization.Roles.Role(user.TenantId, roleName, roleName)
                            {
                                IsStatic = false
                            };
                            CheckErrors(await _roleManager.CreateAsync(role));
                            await _roleManager.SetGrantedPermissionsAsync(role, new[]
                            {
                                new Permission(PermissionNames.Pages_Rider),
                                new Permission(PermissionNames.Pages_Rider_Dashboard),
                                new Permission(PermissionNames.Pages_Rider_OrderHistory)
                            });
                        }
                        CheckErrors(await _userManager.AddToRoleAsync(user, role.Name));
                    }
                    else if (isWaiter)
                    {
                        var roleName = StaticRoleNames.Tenants.Waiter;
                        var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.TenantId == user.TenantId && r.Name == roleName);
                        if (role == null)
                        {
                            role = new SmartPos.Authorization.Roles.Role(user.TenantId, roleName, roleName)
                            {
                                IsStatic = false
                            };
                            CheckErrors(await _roleManager.CreateAsync(role));
                            await _roleManager.SetGrantedPermissionsAsync(role, new[]
                            {
                                new Permission(PermissionNames.Pages_Pos),
                                new Permission(PermissionNames.Pages_DiningTables)
                            });
                        }
                        CheckErrors(await _userManager.AddToRoleAsync(user, role.Name));
                    }
                }
            }

            staff.UserId = user.Id;
            staff.Email = email;
            staff.LoginPassword = password;
            await CurrentUnitOfWork.SaveChangesAsync();

            await _staffHistoryWriter.WriteAsync(
                staff.Id,
                staff.BranchId,
                StaffHistoryAction.Updated,
                $"Login account created for staff '{staff.Name}'.");

            return await GetAsync(new EntityDto<int>(staff.Id));
        }

        public async Task ChangeLoginPasswordAsync(ChangeStaffLoginPasswordDto input)
        {
            CheckUpdatePermission();

            var staff = await Repository.GetAsync(input.StaffId);
            if (!staff.UserId.HasValue)
            {
                throw new UserFriendlyException("No login account exists for this staff member.");
            }

            var password = input.NewPassword ?? string.Empty;
            if (password.Length < 6)
            {
                throw new UserFriendlyException("Password must be at least 6 characters.");
            }

            var user = await _userManager.GetUserByIdAsync(staff.UserId.Value);
            user.Password = _passwordHasher.HashPassword(user, password);
            staff.LoginPassword = password;
            await CurrentUnitOfWork.SaveChangesAsync();

            await _staffHistoryWriter.WriteAsync(
                staff.Id,
                staff.BranchId,
                StaffHistoryAction.Updated,
                $"Login password changed for staff '{staff.Name}'.");
        }

        protected override IQueryable<Staff> CreateFilteredQuery(PagedStaffResultRequestDto input)
        {
            return Repository.GetAll()
                .Include(x => x.Branch)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Phone != null && x.Phone.Contains(input.Keyword))
                         || (x.Email != null && x.Email.Contains(input.Keyword))
                         || (x.EmployeeCode != null && x.EmployeeCode.Contains(input.Keyword))
                         || (x.Designation != null && x.Designation.Contains(input.Keyword)))
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive.Value)
                .WhereIf(input.BranchId.HasValue, x => x.BranchId == input.BranchId.Value);
        }

        protected override async Task<Staff> GetEntityByIdAsync(int id)
        {
            var entity = await Repository.GetAll()
                .Include(x => x.Branch)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                throw new UserFriendlyException("Staff not found");
            }

            return entity;
        }

        protected override StaffDto MapToEntityDto(Staff entity)
        {
            var dto = base.MapToEntityDto(entity);
            dto.BranchName = entity.Branch?.Name;
            return dto;
        }

        private void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }

        private int? ResolveBranchId()
        {
            return BranchQueryHelper.ResolveBranchIdForFilter(
                _branchContext,
                _userRepository,
                AbpSession,
                PermissionChecker);
        }
    }
}
