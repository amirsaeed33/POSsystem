using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using SmartPos.Customers.Dto;
using SmartPos.Orders.Dto;
using SmartPos.Sales.Dto;

namespace SmartPos.Orders
{
    public interface ICustomerOrderAppService : IAsyncCrudAppService<
        CustomerOrderDto,
        int,
        PagedCustomerOrderResultRequestDto,
        CreateCustomerOrderDto,
        CustomerOrderDto>
    {
        Task<SaleDto> ApproveAsync(EntityDto<int> input);

        Task DeliverAsync(EntityDto<int> input);

        /// <summary>Admin: complete order and insert payment/sale.</summary>
        Task CompleteAsync(CompleteCustomerOrderInput input);

        Task RejectAsync(EntityDto<int> input);

        [AbpAllowAnonymous]
        Task<CustomerOrderDto> CreateOnlineOrderAsync(CreateCustomerOrderDto input);

        Task<List<OnlineProductDto>> GetOnlineCatalogAsync(int? branchId);

        Task<OnlineProductDto> GetOnlineProductAsync(int id, int? branchId);

        Task<OnlineStoreHeaderDto> GetOnlineStoreHeaderAsync(int branchId);

        Task<List<OnlineBranchDto>> GetOnlineBranchesAsync();

        /// <summary>
        /// Anonymous: map request host (or explicit host) to the branch storefront for custom domains.
        /// Returns null when the host is the platform or is not mapped.
        /// </summary>
        [AbpAllowAnonymous]
        Task<StorefrontByHostDto> GetStorefrontByHostAsync(string host = null);

        Task<List<string>> GetOnlineCategoriesAsync(int? branchId);

        Task<CustomerOrderDto> OverrideDeliveryFeeAsync(OverrideDeliveryFeeInput input);

        Task<CustomerOrderDto> TrackOrderAsync(string orderNo);

        [AbpAllowAnonymous]
        Task<List<CustomerOrderDto>> GetOrdersByPhoneAsync(CustomerOrderHistoryInput input);

        [AbpAllowAnonymous]
        Task<OnlineCustomerProfileDto> GetOnlineCustomerProfileAsync(string phone, int branchId);

        [AbpAllowAnonymous]
        Task<OnlineCustomerProfileDto> UpsertOnlineCustomerProfileAsync(UpsertOnlineCustomerProfileInput input);

        /// <summary>Purchaser / customer: load by phone. Staff: pass customerId.</summary>
        [AbpAllowAnonymous]
        Task<List<CustomerSavedAddressDto>> GetSavedAddressesAsync(GetCustomerSavedAddressesInput input);

        /// <summary>Save delivery address + zone onto the customer profile.</summary>
        [AbpAllowAnonymous]
        Task<CustomerSavedAddressDto> SaveSavedAddressAsync(SaveCustomerAddressInput input);

        /// <summary>Delete a saved address (owner via phone match, or staff).</summary>
        [AbpAllowAnonymous]
        Task DeleteSavedAddressAsync(DeleteCustomerSavedAddressInput input);

        /// <summary>Product ids favorited by customer (phone + branch).</summary>
        [AbpAllowAnonymous]
        Task<List<int>> GetFavoriteProductIdsAsync(GetCustomerFavoritesInput input);

        /// <summary>Add or remove a favorite product for the customer.</summary>
        [AbpAllowAnonymous]
        Task<ToggleCustomerFavoriteResultDto> ToggleFavoriteAsync(ToggleCustomerFavoriteInput input);

        [AbpAllowAnonymous]
        Task<List<BranchFeedbackDto>> GetBranchFeedbacksAsync(int branchId, int maxResultCount = 20);

        [AbpAllowAnonymous]
        Task<CustomerOrderDto> RateOrderAsync(RateCustomerOrderInput input);

        Task<List<DeliveryRiderStatusDto>> GetAvailableRidersAsync(int? branchId, int? currentOrderId = null);

        Task<CustomerOrderDto> AssignRiderAsync(AssignRiderDto input);
    }
}
