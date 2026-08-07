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
using SmartPos.Authorization.Users;
using SmartPos.Staffs.Dto;

namespace SmartPos.Staffs
{
    [AbpAuthorize(PermissionNames.Pages_Staff)]
    public class StaffAppService : AsyncCrudAppService<Staff, StaffDto, int, PagedStaffResultRequestDto, CreateStaffDto, StaffDto>, IStaffAppService
    {
        private readonly StaffHistoryWriter _staffHistoryWriter;
        private readonly UserManager _userManager;
        private readonly IPasswordHasher<User> _passwordHasher;

        public StaffAppService(
            IRepository<Staff> repository,
            StaffHistoryWriter staffHistoryWriter,
            UserManager userManager,
            IPasswordHasher<User> passwordHasher)
            : base(repository)
        {
            _staffHistoryWriter = staffHistoryWriter;
            _userManager = userManager;
            _passwordHasher = passwordHasher;
        }

        public override async Task<StaffDto> CreateAsync(CreateStaffDto input)
        {
            CheckCreatePermission();

            var entity = MapToEntity(input);
            if (!entity.TenantId.HasValue)
            {
                entity.TenantId = AbpSession.TenantId;
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

            MapToEntity(input, entity);
            entity.UserId = userId;

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
                throw new UserFriendlyException("A login account already exists for this staff member.");
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
                throw new UserFriendlyException("A user with this email already exists.");
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

            staff.UserId = user.Id;
            staff.Email = email;
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
    }
}
