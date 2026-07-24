using Abp.Application.Services;
using SmartPos.Accounts.Dto;

namespace SmartPos.Accounts
{
    public interface IBusinessAccountAppService : IAsyncCrudAppService<BusinessAccountDto, int, PagedBusinessAccountResultRequestDto, CreateBusinessAccountDto, BusinessAccountDto>
    {
    }
}
