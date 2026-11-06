using Abp.Application.Services;
using SmartPos.Developers.Dto;

namespace SmartPos.Developers
{
    public interface IDeveloperAppService : IAsyncCrudAppService<
        DeveloperDto,
        int,
        PagedDeveloperResultRequestDto,
        CreateDeveloperDto,
        DeveloperDto>
    {
    }
}
