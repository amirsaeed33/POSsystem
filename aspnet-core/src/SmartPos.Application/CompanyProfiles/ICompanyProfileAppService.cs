using System.Threading.Tasks;
using Abp.Application.Services;
using SmartPos.CompanyProfiles.Dto;

namespace SmartPos.CompanyProfiles
{
    public interface ICompanyProfileAppService : IAsyncCrudAppService<CompanyProfileDto, int, PagedCompanyProfileResultRequestDto, CreateCompanyProfileDto, CompanyProfileDto>
    {
        Task<CompanyProfileDto> GetCurrentAsync();
    }
}
