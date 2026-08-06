using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Units.Dto;

namespace SmartPos.Units
{
    public interface IUnitAppService : IAsyncCrudAppService<UnitDto, int, PagedUnitResultRequestDto, CreateUnitDto, UnitDto>
    {
        Task<ListResultDto<UnitDto>> GetLookupAsync();
    }
}
