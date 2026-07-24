using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using SmartPos.Authorization;
using SmartPos.Products.Dto;

namespace SmartPos.Products
{
    [AbpAuthorize(PermissionNames.Pages_Products)]
    public class ProductAppService : AsyncCrudAppService<Product, ProductDto, int, PagedProductResultRequestDto, CreateProductDto, ProductDto>, IProductAppService
    {
        public ProductAppService(IRepository<Product> repository)
            : base(repository)
        {
        }

        public override async Task<ProductDto> CreateAsync(CreateProductDto input)
        {
            CheckCreatePermission();

            var product = ObjectMapper.Map<Product>(input);
            product.Barcode = NormalizeBarcode(input.Barcode);
            product.ImagePath = ProductImageStore.SaveBase64Image(input.ImageBase64);

            await Repository.InsertAsync(product);
            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAsync(new EntityDto<int>(product.Id));
        }

        public override async Task<ProductDto> UpdateAsync(ProductDto input)
        {
            CheckUpdatePermission();

            var product = await GetEntityByIdAsync(input.Id);

            product.Name = input.Name;
            product.Description = input.Description;
            product.Barcode = NormalizeBarcode(input.Barcode);
            product.Price = input.Price;
            product.AlertQuantityLimit = input.AlertQuantityLimit;
            product.CategoryId = input.CategoryId;
            product.BrandId = input.BrandId;
            product.UnitId = input.UnitId;

            if (ProductImageStore.IsNewImagePayload(input.ImageBase64))
            {
                ProductImageStore.DeleteIfExists(product.ImagePath);
                product.ImagePath = ProductImageStore.SaveBase64Image(input.ImageBase64);
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAsync(new EntityDto<int>(product.Id));
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var product = await Repository.GetAsync(input.Id);
            ProductImageStore.DeleteIfExists(product.ImagePath);
            await Repository.DeleteAsync(product);
        }

        protected override IQueryable<Product> CreateFilteredQuery(PagedProductResultRequestDto input)
        {
            return Repository.GetAllIncluding(x => x.Category, x => x.Brand, x => x.Unit)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Description != null && x.Description.Contains(input.Keyword))
                         || (x.Barcode != null && x.Barcode.Contains(input.Keyword)))
                .WhereIf(input.CategoryId.HasValue, x => x.CategoryId == input.CategoryId.Value)
                .WhereIf(input.BrandId.HasValue, x => x.BrandId == input.BrandId.Value)
                .WhereIf(input.UnitId.HasValue, x => x.UnitId == input.UnitId.Value);
        }

        protected override async Task<Product> GetEntityByIdAsync(int id)
        {
            return await AsyncQueryableExecuter.FirstOrDefaultAsync(
                Repository.GetAllIncluding(x => x.Category, x => x.Brand, x => x.Unit)
                    .Where(x => x.Id == id));
        }

        private static string NormalizeBarcode(string barcode)
        {
            return barcode.IsNullOrWhiteSpace() ? null : barcode.Trim();
        }
    }
}
