using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using SmartPos.Customers.Dto;
using SmartPos.Products.Dto;
using SmartPos.Sales.Dto;

namespace SmartPos.Sales
{
    public interface ISaleAppService : IAsyncCrudAppService<SaleDto, int, PagedSaleResultRequestDto, CreateSaleDto, SaleDto>
    {
        Task<ProductDto> GetProductByBarcodeAsync(string barcode);

        /// <summary>
        /// POS lookup: exact barcode, then exact name, then unique partial name match.
        /// </summary>
        Task<ProductDto> GetPosProductAsync(string keyword);

        Task<ListResultDto<ProductDto>> GetPosProductSuggestionsAsync(string keyword, int? categoryId = null);

        /// <summary>
        /// Customers for POS / sale entry. Requires Pages.Sales only (not Pages.Customers).
        /// </summary>
        Task<ListResultDto<CustomerDto>> GetPosCustomersAsync();

        /// <summary>
        /// Products for sale entry forms. Requires Pages.Sales only (not Pages.Products).
        /// </summary>
        Task<ListResultDto<ProductDto>> GetPosProductsAsync(int? categoryId = null);

        Task<ListResultDto<ProductDto>> GetTopSellingProductsAsync(int maxCount = 5);

        [AbpAllowAnonymous]
        [RemoteService(false)]
        Task<SaleDto> CreateSaleInternalAsync(CreateSaleDto input);

        [AbpAllowAnonymous]
        [RemoteService(false)]
        Task<SaleDto> CreateSaleForCustomerOrderAsync(int orderId, decimal? paymentReceivedAmount = null);

        [AbpAllowAnonymous]
        [RemoteService(false)]
        Task SyncDeliveredCustomerOrderSalesAsync(int? branchId = null);

        Task<SaleDto> CollectPaymentAsync(CollectSalePaymentDto input);
    }
}
