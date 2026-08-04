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
using SmartPos.Staffs.Dto;

namespace SmartPos.Staffs
{
    [AbpAuthorize(PermissionNames.Pages_Staff)]
    public class StaffAppService : AsyncCrudAppService<Staff, StaffDto, int, PagedStaffResultRequestDto, CreateStaffDto, StaffDto>, IStaffAppService
    {
        public StaffAppService(IRepository<Staff> repository)
            : base(repository)
        {
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

            return await GetAsync(new EntityDto<int>(entity.Id));
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
    }
}
