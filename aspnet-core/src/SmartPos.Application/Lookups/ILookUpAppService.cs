using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Lookups.Dto;

namespace SmartPos.Lookups
{
    public interface ILookUpAppService :
        IAsyncCrudAppService<LookUpDto, int, PagedLookUpResultRequestDto, CreateLookUpDto, LookUpDto>
    {
        Task<ListResultDto<LookUpDto>> GetByTypeAsync(string type);
    }
}
