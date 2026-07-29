using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
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
    }
}
