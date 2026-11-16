using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Restaurant.Dto;

namespace SmartPos.Restaurant
{
    public interface IRestaurantTableAppService : IApplicationService
    {
        Task<List<RestaurantTableDto>> GetTablesAsync(int branchId);

        Task<RestaurantTableDto> GetAsync(EntityDto<int> input);

        Task<RestaurantTableDto> CreateAsync(CreateRestaurantTableDto input);

        Task<RestaurantTableDto> UpdateAsync(RestaurantTableDto input);

        Task DeleteAsync(EntityDto<int> input);

        Task<RestaurantTableDto> ChangeStatusAsync(ChangeTableStatusDto input);

        Task TransferTableAsync(int fromTableId, int toTableId);

        Task<RestaurantTableDto> AssignWaiterAsync(AssignWaiterDto input);
    }
}
