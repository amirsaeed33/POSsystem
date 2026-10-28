using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Restaurant.Dto;

namespace SmartPos.Restaurant
{
    public interface IRestaurantOrderAppService : IApplicationService
    {
        Task<List<RestaurantOrderDto>> GetActiveOrdersAsync(int branchId);

        Task<RestaurantOrderDto> GetAsync(EntityDto<int> input);

        Task<RestaurantOrderDto> GetByTableAsync(int tableId);

        Task<RestaurantOrderDto> SaveOrderAsync(CreateOrUpdateRestaurantOrderDto input);

        Task<RestaurantOrderDto> ReserveCarServiceAsync(ReserveCarServiceDto input);

        Task<List<RestaurantOrderDto>> GetActiveCarOrdersAsync(int branchId);

        Task<SendToKitchenResultDto> SendToKitchenAsync(int orderId);

        Task<SendToKitchenResultDto> ConfirmKotPrintAsync(ConfirmKotPrintDto input);

        Task<RestaurantOrderDto> PrintBillAsync(int orderId);

        Task<int> SettleOrderAsync(SettleRestaurantOrderDto input);

        Task CancelOrderAsync(int orderId);

        Task<RestaurantOrderDto> SplitBillAsync(SplitBillDto input);

        Task<RestaurantPrintLogDto> RecordPrintLogAsync(RecordPrintLogDto input);

        Task<List<RestaurantPrintLogDto>> GetOrderPrintLogsAsync(int orderId);

        Task<List<UnpaidTakeawayOrderDto>> GetUnpaidTakeawayOrdersAsync(int branchId);
    }
}
