using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class RestaurantService
{
    private readonly AbpApiClient _api;

    public RestaurantService(AbpApiClient api)
    {
        _api = api;
    }

    public Task<List<RestaurantTableDto>> GetTablesAsync(int branchId) =>
        _api.GetAsync<List<RestaurantTableDto>>($"api/services/app/RestaurantTable/GetTables?branchId={branchId}");

    public Task AssignWaiterAsync(AssignWaiterDto input) =>
        _api.PostAsync("api/services/app/RestaurantTable/AssignWaiter", input);

    public Task<List<RestaurantOrderDto>> GetActiveOrdersAsync(int branchId) =>
        _api.GetAsync<List<RestaurantOrderDto>>($"api/services/app/RestaurantOrder/GetActiveOrders?branchId={branchId}");

    public Task<RestaurantOrderDto> GetOrderAsync(int id) =>
        _api.GetAsync<RestaurantOrderDto>($"api/services/app/RestaurantOrder/Get?Id={id}");

    public Task<RestaurantOrderDto> GetByTableAsync(int tableId) =>
        _api.GetAsync<RestaurantOrderDto>($"api/services/app/RestaurantOrder/GetByTable?tableId={tableId}");

    public Task<RestaurantOrderDto> OpenTableSessionAsync(OpenTableSessionDto input) =>
        _api.PostAsync<RestaurantOrderDto>("api/services/app/RestaurantOrder/OpenTableSession", input);

    public Task<RestaurantOrderDto> SaveOrderAsync(CreateOrUpdateRestaurantOrderDto input) =>
        _api.PostAsync<RestaurantOrderDto>("api/services/app/RestaurantOrder/SaveOrder", input);

    public Task<SendToKitchenResultDto> SendToKitchenAsync(int orderId) =>
        _api.PostAsync<SendToKitchenResultDto>($"api/services/app/RestaurantOrder/SendToKitchen?orderId={orderId}", null);

    public Task<SendToKitchenResultDto> ConfirmKotPrintAsync(ConfirmKotPrintDto input) =>
        _api.PostAsync<SendToKitchenResultDto>("api/services/app/RestaurantOrder/ConfirmKotPrint", input);

    public Task<RestaurantOrderDto> PrintBillAsync(int orderId) =>
        _api.PostAsync<RestaurantOrderDto>($"api/services/app/RestaurantOrder/PrintBill?orderId={orderId}", null);

    public Task<int> SettleOrderAsync(SettleRestaurantOrderDto input) =>
        _api.PostAsync<int>("api/services/app/RestaurantOrder/SettleOrder", input);

    public Task CancelOrderAsync(int orderId) =>
        _api.PostAsync($"api/services/app/RestaurantOrder/CancelOrder?orderId={orderId}", null);

    public Task<RestaurantOrderDto> SplitBillAsync(SplitBillDto input) =>
        _api.PostAsync<RestaurantOrderDto>("api/services/app/RestaurantOrder/SplitBill", input);

    public async Task<List<ProductDto>> GetPosProductsAsync(int? categoryId = null)
    {
        var url = "api/services/app/Sale/GetPosProducts";
        if (categoryId.HasValue)
        {
            url += $"?categoryId={categoryId}";
        }

        var result = await _api.GetAsync<AbpListResult<ProductDto>>(url);
        return result.Items ?? new List<ProductDto>();
    }

    public async Task<List<ProductModifierGroupDto>> GetModifiersForProductAsync(int productId)
    {
        try
        {
            return await _api.GetAsync<List<ProductModifierGroupDto>>(
                $"api/services/app/ProductModifier/GetByProduct?productId={productId}");
        }
        catch
        {
            return new List<ProductModifierGroupDto>();
        }
    }

    public async Task<DealDto?> GetDealByProductAsync(int productId)
    {
        try
        {
            return await _api.GetAsync<DealDto>($"api/services/app/Deal/GetByProductId?productId={productId}");
        }
        catch
        {
            return null;
        }
    }
}
