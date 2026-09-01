using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
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

        Task<ListResultDto<ProductDto>> GetPosProductSuggestionsAsync(string keyword);

        /// <summary>
        /// Customers for POS / sale entry. Requires Pages.Sales only (not Pages.Customers).
        /// </summary>
        Task<ListResultDto<CustomerDto>> GetPosCustomersAsync();

        /// <summary>
        /// Products for sale entry forms. Requires Pages.Sales only (not Pages.Products).
        /// </summary>
        Task<ListResultDto<ProductDto>> GetPosProductsAsync();

        Task<ListResultDto<ProductDto>> GetTopSellingProductsAsync(int maxCount = 5);

        Task<SaleDto> CreateSaleInternalAsync(CreateSaleDto input);
    }
}
