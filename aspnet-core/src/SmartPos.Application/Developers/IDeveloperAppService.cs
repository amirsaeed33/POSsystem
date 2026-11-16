using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
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
        /// <summary>Anonymous: active developers for the public product landing page.</summary>
        [AbpAllowAnonymous]
        Task<List<DeveloperDto>> GetPublicDevelopersAsync();
    }
}
