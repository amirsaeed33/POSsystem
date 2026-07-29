using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Products.Dto;

namespace SmartPos.Products
{
    [AbpAuthorize(PermissionNames.Pages_Products)]
    public class ProductAppService : AsyncCrudAppService<Product, ProductDto, int, PagedProductResultRequestDto, CreateProductDto, ProductDto>, IProductAppService
    {
        private const string DuplicateBarcodeMessage =
            "Barcode already exists. Please enter a unique barcode.";

        public ProductAppService(IRepository<Product> repository)
            : base(repository)
        {
        }

        public override async Task<ProductDto> CreateAsync(CreateProductDto input)
        {
            CheckCreatePermission();

            EnsureValidPricing(input.Price, input.WholesalePrice, input.CostPrice);

            var barcode = NormalizeBarcode(input.Barcode);
            await EnsureBarcodeIsUniqueAsync(barcode, excludeProductId: null);

            var product = ObjectMapper.Map<Product>(input);
            product.TenantId = AbpSession.TenantId;
            product.Barcode = barcode;
            product.ImagePath = ProductImageStore.SaveBase64Image(input.ImageBase64);

            try
            {
                await Repository.InsertAsync(product);
                await CurrentUnitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateBarcodeViolation(ex))
            {
                throw new UserFriendlyException(DuplicateBarcodeMessage);
            }

            return await GetAsync(new EntityDto<int>(product.Id));
        }

        public override async Task<ProductDto> UpdateAsync(ProductDto input)
        {
            CheckUpdatePermission();

            var product = await GetEntityByIdAsync(input.Id);

            EnsureValidPricing(input.Price, input.WholesalePrice, input.CostPrice);

            var barcode = NormalizeBarcode(input.Barcode);
            await EnsureBarcodeIsUniqueAsync(barcode, excludeProductId: product.Id);

            product.Name = input.Name;
            product.Description = input.Description;
            product.Barcode = barcode;
            product.Price = input.Price;
            product.WholesalePrice = input.WholesalePrice;
            product.CostPrice = input.CostPrice;
            product.AlertQuantityLimit = input.AlertQuantityLimit;
            product.CategoryId = input.CategoryId;
            product.BrandId = input.BrandId;
            product.UnitId = input.UnitId;

            if (ProductImageStore.IsNewImagePayload(input.ImageBase64))
            {
                ProductImageStore.DeleteIfExists(product.ImagePath);
                product.ImagePath = ProductImageStore.SaveBase64Image(input.ImageBase64);
            }

            try
            {
                await CurrentUnitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateBarcodeViolation(ex))
            {
                throw new UserFriendlyException(DuplicateBarcodeMessage);
            }

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

        private static void EnsureValidPricing(decimal price, decimal wholesalePrice, decimal costPrice)
        {
            if (price < costPrice)
            {
                throw new UserFriendlyException(
                    "Selling price cannot be lower than the cost price.");
            }

            if (price < wholesalePrice)
            {
                throw new UserFriendlyException(
                    "Selling price cannot be lower than the wholesale price.");
            }
        }

        private async Task EnsureBarcodeIsUniqueAsync(string barcode, int? excludeProductId)
        {
            if (barcode.IsNullOrWhiteSpace())
            {
                return;
            }

            // Match current tenant (including host null) and ignore soft-deleted rows via ABP filters.
            var query = Repository.GetAll().Where(x => x.Barcode == barcode);
            if (excludeProductId.HasValue)
            {
                query = query.Where(x => x.Id != excludeProductId.Value);
            }

            if (await AsyncQueryableExecuter.AnyAsync(query))
            {
                throw new UserFriendlyException(DuplicateBarcodeMessage);
            }
        }

        private static bool IsDuplicateBarcodeViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message ?? string.Empty;
            return message.IndexOf("IX_AppProducts_TenantId_Barcode", StringComparison.OrdinalIgnoreCase) >= 0
                   || message.IndexOf("duplicate key", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NormalizeBarcode(string barcode)
        {
            return barcode.IsNullOrWhiteSpace() ? null : barcode.Trim();
        }
    }
}
