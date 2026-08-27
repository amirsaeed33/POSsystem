using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Products.Dto;

namespace SmartPos.Products
{
    public interface IProductAppService : IAsyncCrudAppService<ProductDto, int, PagedProductResultRequestDto, CreateProductDto, ProductDto>
    {
        Task<ProductImportResultDto> ImportProductsAsync(List<ImportProductRowDto> inputs);
    }

    public class ImportProductRowDto
    {
        public string Name { get; set; }
        public string Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal StockQuantity { get; set; }
        public decimal AlertQuantityLimit { get; set; } = 10;
        public string CategoryName { get; set; }
        public string BrandName { get; set; }
        public string UnitName { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
    }

    public class ProductImportResultDto
    {
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
    }
}
