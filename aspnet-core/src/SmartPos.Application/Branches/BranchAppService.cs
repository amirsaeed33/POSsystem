using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Branches.Dto;

namespace SmartPos.Branches
{
    [AbpAuthorize]
    public class BranchAppService : AsyncCrudAppService<Branch, BranchDto, int, PagedBranchResultRequestDto, CreateBranchDto, BranchDto>, IBranchAppService
    {
        private readonly IRepository<UserBranch> _userBranchRepository;

        public BranchAppService(
            IRepository<Branch> repository,
            IRepository<UserBranch> userBranchRepository)
            : base(repository)
        {
            _userBranchRepository = userBranchRepository;
            LocalizationSourceName = SmartPosConsts.LocalizationSourceName;
            GetPermissionName = PermissionNames.Pages_Branches;
            GetAllPermissionName = PermissionNames.Pages_Branches;
            CreatePermissionName = PermissionNames.Pages_Branches;
            UpdatePermissionName = PermissionNames.Pages_Branches;
            DeletePermissionName = PermissionNames.Pages_Branches;
        }

        public override async Task<BranchDto> CreateAsync(CreateBranchDto input)
        {
            CheckCreatePermission();

            var code = NormalizeCode(input.Code);
            await EnsureCodeIsUniqueAsync(code, excludeBranchId: null);

            var branch = ObjectMapper.Map<Branch>(input);
            branch.TenantId = AbpSession.TenantId;
            branch.Code = code;
            branch.IsActive = input.IsActive;
            branch.IsDefault = false;

            await Repository.InsertAsync(branch);
            await CurrentUnitOfWork.SaveChangesAsync();

            if (AbpSession.UserId.HasValue)
            {
                var alreadyAssigned = await _userBranchRepository.GetAll()
                    .AnyAsync(x => x.UserId == AbpSession.UserId.Value && x.BranchId == branch.Id);
                if (!alreadyAssigned)
                {
                    await _userBranchRepository.InsertAsync(new UserBranch
                    {
                        TenantId = AbpSession.TenantId,
                        UserId = AbpSession.UserId.Value,
                        BranchId = branch.Id
                    });
                    await CurrentUnitOfWork.SaveChangesAsync();
                }
            }

            return MapToEntityDto(branch);
        }

        public override async Task<BranchDto> UpdateAsync(BranchDto input)
        {
            CheckUpdatePermission();

            var branch = await Repository.GetAsync(input.Id);
            var code = NormalizeCode(input.Code);
            await EnsureCodeIsUniqueAsync(code, excludeBranchId: branch.Id);

            if (branch.IsDefault && !input.IsActive)
            {
                throw new UserFriendlyException("The default branch cannot be deactivated.");
            }

            branch.Name = input.Name;
            branch.Code = code;
            branch.IsActive = input.IsActive;

            await CurrentUnitOfWork.SaveChangesAsync();
            return MapToEntityDto(branch);
        }

        /// <summary>
        /// Returns active branches assigned to the current user (for switcher / forms).
        /// </summary>
        public async Task<ListResultDto<BranchDto>> GetLookupAsync()
        {
            if (!AbpSession.UserId.HasValue)
            {
                return new ListResultDto<BranchDto>();
            }

            var assignedBranchIds = await _userBranchRepository.GetAll()
                .Where(x => x.UserId == AbpSession.UserId.Value)
                .Select(x => x.BranchId)
                .ToListAsync();

            var branches = await Repository.GetAll()
                .Where(x => assignedBranchIds.Contains(x.Id) && x.IsActive)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return new ListResultDto<BranchDto>(ObjectMapper.Map<List<BranchDto>>(branches));
        }

        protected override IQueryable<Branch> CreateFilteredQuery(PagedBranchResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword) || x.Code.Contains(input.Keyword))
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive.Value);
        }

        private static string NormalizeCode(string code)
        {
            return (code ?? string.Empty).Trim().ToUpperInvariant();
        }

        private async Task EnsureCodeIsUniqueAsync(string code, int? excludeBranchId)
        {
            var exists = await Repository.GetAll()
                .AnyAsync(x => x.Code == code && (!excludeBranchId.HasValue || x.Id != excludeBranchId.Value));

            if (exists)
            {
                throw new UserFriendlyException("Branch code already exists. Please enter a unique code.");
            }
        }
    }
}
