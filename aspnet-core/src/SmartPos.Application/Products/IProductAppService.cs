using Abp.Application.Services;
using SmartPos.Products.Dto;

namespace SmartPos.Products
{
    public interface IProductAppService : IAsyncCrudAppService<ProductDto, int, PagedProductResultRequestDto, CreateProductDto, ProductDto>
    {
    }
}
