using Abp.Application.Services;
using SmartPos.MultiTenancy.Dto;

namespace SmartPos.MultiTenancy
{
    public interface ITenantAppService : IAsyncCrudAppService<TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>
    {
    }
}

