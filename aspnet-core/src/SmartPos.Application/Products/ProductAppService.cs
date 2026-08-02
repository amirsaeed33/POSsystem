using System;
using System.Collections.Generic;
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
using SmartPos.Branches;
using SmartPos.Inventory;
using SmartPos.Products.Dto;

namespace SmartPos.Products
{
    [AbpAuthorize(PermissionNames.Pages_Products)]
    public class ProductAppService : AsyncCrudAppService<Product, ProductDto, int, PagedProductResultRequestDto, CreateProductDto, ProductDto>, IProductAppService
    {
        private const string DuplicateBarcodeMessage =
            "Barcode already exists. Please enter a unique barcode.";

        private readonly IBranchStockManager _branchStockManager;
        private readonly IBranchContext _branchContext;

        public ProductAppService(
            IRepository<Product> repository,
            IBranchStockManager branchStockManager,
            IBranchContext branchContext)
            : base(repository)
        {
            _branchStockManager = branchStockManager;
            _branchContext = branchContext;
        }

        public override async Task<PagedResultDto<ProductDto>> GetAllAsync(PagedProductResultRequestDto input)
        {
            var result = await base.GetAllAsync(input);
            await PopulateStockFieldsAsync(result.Items);
            return result;
        }

        public override async Task<ProductDto> GetAsync(EntityDto<int> input)
        {
            var dto = await base.GetAsync(input);
            await PopulateStockFieldsAsync(new[] { dto });
            return dto;
        }

        public override async Task<ProductDto> CreateAsync(CreateProductDto input)
        {
            CheckCreatePermission();

            EnsureRequiredFields(
                input.Name,
                input.Barcode,
                input.Price,
                input.WholesalePrice,
                input.CostPrice,
                input.AlertQuantityLimit,
                input.CategoryId,
                input.BrandId,
                input.UnitId);
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

            EnsureRequiredFields(
                input.Name,
                input.Barcode,
                input.Price,
                input.WholesalePrice,
                input.CostPrice,
                input.AlertQuantityLimit,
                input.CategoryId,
                input.BrandId,
                input.UnitId);
            EnsureValidPricing(input.Price, input.WholesalePrice, input.CostPrice);

            var barcode = NormalizeBarcode(input.Barcode);
            await EnsureBarcodeIsUniqueAsync(barcode, excludeProductId: product.Id);

            product.Name = input.Name;
            product.Description = input.Description;
            product.Location = input.Location;
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
                         || (x.Location != null && x.Location.Contains(input.Keyword))
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

        private async Task PopulateStockFieldsAsync(IReadOnlyList<ProductDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
            {
                return;
            }

            var branchId = _branchContext.BranchId ?? 0;
            if (branchId <= 0)
            {
                foreach (var dto in dtos)
                {
                    dto.StockQuantity = 0;
                    dto.StockProfit = 0;
                }

                return;
            }

            var quantities = await _branchStockManager.GetQuantitiesAsync(
                branchId,
                dtos.Select(x => x.Id));

            foreach (var dto in dtos)
            {
                dto.StockQuantity = quantities.TryGetValue(dto.Id, out var qty) ? qty : 0;
                dto.StockProfit = ProductPricing.StockProfit(dto.Price, dto.CostPrice, dto.StockQuantity);
            }
        }

        private static void EnsureRequiredFields(
            string name,
            string barcode,
            decimal price,
            decimal wholesalePrice,
            decimal costPrice,
            decimal alertQuantityLimit,
            int categoryId,
            int brandId,
            int unitId)
        {
            if (name.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("Name is required.");
            }

            if (barcode.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("Barcode is required.");
            }

            if (price <= 0)
            {
                throw new UserFriendlyException("Price is required.");
            }

            if (wholesalePrice < 0)
            {
                throw new UserFriendlyException("Wholesale price is required.");
            }

            if (costPrice < 0)
            {
                throw new UserFriendlyException("Cost price is required.");
            }

            if (alertQuantityLimit < 0)
            {
                throw new UserFriendlyException("Alert quantity limit is required.");
            }

            if (categoryId <= 0)
            {
                throw new UserFriendlyException("Category is required.");
            }

            if (brandId <= 0)
            {
                throw new UserFriendlyException("Brand is required.");
            }

            if (unitId <= 0)
            {
                throw new UserFriendlyException("Unit is required.");
            }
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
