using Abp.Application.Services;
using SmartPos.Units.Dto;

namespace SmartPos.Units
{
    public interface IUnitAppService : IAsyncCrudAppService<UnitDto, int, PagedUnitResultRequestDto, CreateUnitDto, UnitDto>
    {
    }
}
