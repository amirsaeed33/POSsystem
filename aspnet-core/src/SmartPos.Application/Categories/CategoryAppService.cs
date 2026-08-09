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
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Categories.Dto;

namespace SmartPos.Categories
{
    [AbpAuthorize]
    public class CategoryAppService : AsyncCrudAppService<Category, CategoryDto, int, PagedCategoryResultRequestDto, CreateCategoryDto, CategoryDto>, ICategoryAppService
    {
        private readonly IBranchContext _branchContext;
        private readonly IRepository<User, long> _userRepository;

        public CategoryAppService(
            IRepository<Category> repository,
            IBranchContext branchContext,
            IRepository<User, long> userRepository)
            : base(repository)
        {
            _branchContext = branchContext;
            _userRepository = userRepository;
            CreatePermissionName = PermissionNames.Pages_Categories;
            UpdatePermissionName = PermissionNames.Pages_Categories;
            DeletePermissionName = PermissionNames.Pages_Categories;
            GetPermissionName = PermissionNames.Pages_Categories;
            GetAllPermissionName = PermissionNames.Pages_Categories;
        }

        public override async Task<CategoryDto> CreateAsync(CreateCategoryDto input)
        {
            CheckCreatePermission();
            var branchId = RequireCurrentBranchId();
            var entity = ObjectMapper.Map<Category>(input);
            entity.TenantId = AbpSession.TenantId;
            entity.BranchId = branchId;
            entity.IsActive = input.IsActive;
            await Repository.InsertAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();
            return MapToEntityDto(entity);
        }

        [AbpAuthorize(PermissionNames.Pages_Categories, PermissionNames.Pages_Products)]
        public async Task<ListResultDto<CategoryDto>> GetLookupAsync()
        {
            var branchId = ResolveBranchId();
            var query = Repository.GetAll().Where(x => x.IsActive);
            if (branchId.HasValue)
            {
                query = query.Where(x => x.BranchId == branchId.Value);
            }

            var items = await query.OrderBy(x => x.Name).ToListAsync();
            return new ListResultDto<CategoryDto>(ObjectMapper.Map<List<CategoryDto>>(items));
        }

        protected override IQueryable<Category> CreateFilteredQuery(PagedCategoryResultRequestDto input)
        {
            var branchId = ResolveBranchId();
            return Repository.GetAll()
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Description != null && x.Description.Contains(input.Keyword)));
        }

        private int? ResolveBranchId()
        {
            return BranchQueryHelper.ResolveBranchIdForFilter(
                _branchContext,
                _userRepository,
                AbpSession,
                PermissionChecker);
        }

        private int RequireCurrentBranchId()
        {
            var branchId = ResolveBranchId();
            if (!branchId.HasValue)
            {
                throw new UserFriendlyException("Select a branch before managing categories.");
            }

            return branchId.Value;
        }
    }
}
