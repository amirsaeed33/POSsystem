using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Restaurant.Dto;

namespace SmartPos.Restaurant
{
    public interface IProductModifierAppService : IApplicationService
    {
        Task<List<ProductModifierGroupDto>> GetByProductAsync(int productId);

        Task<ProductModifierGroupDto> CreateGroupAsync(CreateModifierGroupDto input);

        Task DeleteGroupAsync(EntityDto<int> input);

        Task<ProductModifierOptionDto> AddOptionAsync(int groupId, CreateModifierOptionDto input);

        Task DeleteOptionAsync(EntityDto<int> input);
    }
}
