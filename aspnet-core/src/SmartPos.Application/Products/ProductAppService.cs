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

        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchStockManager _branchStockManager;
        private readonly IRepository<BranchStock> _branchStockRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IPermissionChecker _permissionChecker;
        private int? _effectiveBranchIdForQuery;

        public ProductAppService(
            IRepository<Product> repository,
            IBranchAccessChecker branchAccessChecker,
            IBranchStockManager branchStockManager,
            IRepository<BranchStock> branchStockRepository,
            IRepository<Branch> branchRepository,
            IPermissionChecker permissionChecker)
            : base(repository)
        {
            _branchAccessChecker = branchAccessChecker;
            _branchStockManager = branchStockManager;
            _branchStockRepository = branchStockRepository;
            _branchRepository = branchRepository;
            _permissionChecker = permissionChecker;
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

            var canManageBranches = await CanManageBranchesAsync();
            var selectedBranchIds = (input.BranchIds ?? new List<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();
            // Empty branch selection = tenant-level (no BranchStock rows → visible everywhere).
            var targetBranchIds = await ResolveTargetBranchIdsForCreateAsync(
                canManageBranches,
                selectedBranchIds);

            var initialStock = input.StockQuantity > 0 ? input.StockQuantity : 0;
            var product = ObjectMapper.Map<Product>(input);
            product.TenantId = AbpSession.TenantId;
            product.Barcode = barcode;
            product.StockQuantity = 0;
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

            foreach (var branchId in targetBranchIds)
            {
                await _branchStockManager.UpsertStockAndPricesAsync(
                    branchId,
                    product.Id,
                    initialStock,
                    product.Price,
                    product.WholesalePrice,
                    product.CostPrice);
            }

            // Skip visibility check: product may be assigned to branches other than the current topbar branch.
            return await MapProductDtoByIdAsync(product.Id);
        }

        public override async Task<ProductDto> UpdateAsync(ProductDto input)
        {
            CheckUpdatePermission();

            var product = await GetEntityByIdAsync(input.Id);
            await EnsureProductVisibleAsync(product);

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

            var canManageBranches = await CanManageBranchesAsync();
            if (canManageBranches)
            {
                await SyncAssignmentsOnUpdateAsync(product, input.BranchIds);
            }

            try
            {
                await CurrentUnitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateBarcodeViolation(ex))
            {
                throw new UserFriendlyException(DuplicateBarcodeMessage);
            }

            var branchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();
            var assignedBranchIds = await _branchStockManager.GetAssignedBranchIdsAsync(product.Id);
            if (!assignedBranchIds.Any() || assignedBranchIds.Contains(branchId))
            {
                await _branchStockManager.SetPricesAsync(
                    branchId,
                    product.Id,
                    input.Price,
                    input.WholesalePrice,
                    input.CostPrice);
            }

            // Skip visibility check: assignment may exclude the current topbar branch.
            return await MapProductDtoByIdAsync(product.Id);
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var product = await Repository.GetAsync(input.Id);
            await EnsureProductVisibleAsync(product);
            ProductImageStore.DeleteIfExists(product.ImagePath);
            await Repository.DeleteAsync(product);
        }

        public override async Task<ProductDto> GetAsync(EntityDto<int> input)
        {
            var product = await GetEntityByIdAsync(input.Id);
            if (product == null)
            {
                throw new UserFriendlyException("Product not found.");
            }

            await EnsureProductVisibleAsync(product);
            return await MapProductDtoAsync(product);
        }

        private async Task<ProductDto> MapProductDtoByIdAsync(int id)
        {
            var product = await GetEntityByIdAsync(id);
            if (product == null)
            {
                throw new UserFriendlyException("Product not found.");
            }

            return await MapProductDtoAsync(product);
        }

        private async Task<ProductDto> MapProductDtoAsync(Product product)
        {
            var dto = MapToEntityDto(product);
            await OverlayBranchStockAsync(new[] { dto });
            await PopulateBranchIdsAsync(new[] { dto });
            return dto;
        }

        public override async Task<PagedResultDto<ProductDto>> GetAllAsync(PagedProductResultRequestDto input)
        {
            _effectiveBranchIdForQuery = await _branchAccessChecker.GetEffectiveBranchIdAsync();
            try
            {
                var result = await base.GetAllAsync(input);
                await OverlayBranchStockAsync(result.Items);
                await PopulateBranchIdsAsync(result.Items);
                return result;
            }
            finally
            {
                _effectiveBranchIdForQuery = null;
            }
        }

        protected override IQueryable<Product> CreateFilteredQuery(PagedProductResultRequestDto input)
        {
            var query = Repository.GetAllIncluding(x => x.Category, x => x.Brand, x => x.Unit)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Description != null && x.Description.Contains(input.Keyword))
                         || (x.Location != null && x.Location.Contains(input.Keyword))
                         || (x.Barcode != null && x.Barcode.Contains(input.Keyword)))
                .WhereIf(input.CategoryId.HasValue, x => x.CategoryId == input.CategoryId.Value)
                .WhereIf(input.BrandId.HasValue, x => x.BrandId == input.BrandId.Value)
                .WhereIf(input.UnitId.HasValue, x => x.UnitId == input.UnitId.Value);

            if (!_effectiveBranchIdForQuery.HasValue)
            {
                return query.Where(x => false);
            }

            return query.WhereVisibleToBranch(
                _branchStockRepository.GetAll(),
                _effectiveBranchIdForQuery.Value);
        }

        protected override async Task<Product> GetEntityByIdAsync(int id)
        {
            return await AsyncQueryableExecuter.FirstOrDefaultAsync(
                Repository.GetAllIncluding(x => x.Category, x => x.Brand, x => x.Unit)
                    .Where(x => x.Id == id));
        }

        private async Task EnsureProductVisibleAsync(Product product)
        {
            var branchId = await _branchAccessChecker.GetEffectiveBranchIdAsync();
            if (!branchId.HasValue)
            {
                throw new UserFriendlyException("No branch is assigned. Please contact your administrator.");
            }

            var assignedBranchIds = await _branchStockManager.GetAssignedBranchIdsAsync(product.Id);
            if (!assignedBranchIds.Any() || assignedBranchIds.Contains(branchId.Value))
            {
                return;
            }

            throw new UserFriendlyException("Product is not available for this branch.");
        }

        private async Task<bool> CanManageBranchesAsync()
        {
            return await _permissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches);
        }

        private async Task<List<int>> ResolveTargetBranchIdsForCreateAsync(
            bool canManageBranches,
            List<int> branchIds)
        {
            if (!canManageBranches)
            {
                var ownBranchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();
                return new List<int> { ownBranchId };
            }

            var selected = (branchIds ?? new List<int>()).Where(x => x > 0).Distinct().ToList();
            if (!selected.Any())
            {
                // Tenant-level: no BranchStock rows (visible in every location).
                return new List<int>();
            }

            await EnsureBranchesExistAndAccessibleAsync(selected);
            return selected;
        }

        private async Task SyncAssignmentsOnUpdateAsync(
            Product product,
            List<int> branchIds)
        {
            var selected = (branchIds ?? new List<int>()).Where(x => x > 0).Distinct().ToList();
            var current = await _branchStockManager.GetAssignedBranchIdsAsync(product.Id);

            // Empty selection = tenant-level: ensure stock rows exist for every active branch.
            if (!selected.Any())
            {
                var allBranchIds = await GetActiveBranchIdsAsync();
                foreach (var branchId in allBranchIds.Except(current))
                {
                    await _branchStockManager.UpsertStockAndPricesAsync(
                        branchId,
                        product.Id,
                        0,
                        product.Price,
                        product.WholesalePrice,
                        product.CostPrice);
                }

                return;
            }

            await EnsureBranchesExistAndAccessibleAsync(selected);

            var toAdd = selected.Except(current).ToList();
            var toRemove = current.Except(selected).ToList();

            foreach (var branchId in toAdd)
            {
                await _branchStockManager.UpsertStockAndPricesAsync(
                    branchId,
                    product.Id,
                    0,
                    product.Price,
                    product.WholesalePrice,
                    product.CostPrice);
            }

            foreach (var branchId in toRemove)
            {
                await _branchStockManager.RemoveAssignmentAsync(branchId, product.Id);
            }
        }

        private async Task<List<int>> GetActiveBranchIdsAsync()
        {
            return await _branchRepository.GetAll()
                .Where(x => x.IsActive)
                .Select(x => x.Id)
                .ToListAsync();
        }

        private async Task EnsureBranchesExistAndAccessibleAsync(IReadOnlyList<int> branchIds)
        {
            var activeIds = await GetActiveBranchIdsAsync();
            foreach (var branchId in branchIds)
            {
                if (!activeIds.Contains(branchId))
                {
                    throw new UserFriendlyException($"Branch id {branchId} was not found or is inactive.");
                }

                await _branchAccessChecker.EnsureCanAccessBranchAsync(branchId);
            }
        }

        private async Task PopulateBranchIdsAsync(IReadOnlyList<ProductDto> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var activeBranchIds = await GetActiveBranchIdsAsync();
            var productIds = items.Select(x => x.Id).ToList();
            var assignments = await _branchStockRepository.GetAll()
                .Where(x => productIds.Contains(x.ProductId))
                .Select(x => new { x.ProductId, x.BranchId })
                .ToListAsync();

            var map = assignments
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.BranchId).Distinct().ToList());

            foreach (var item in items)
            {
                var assigned = map.TryGetValue(item.Id, out var branchIds)
                    ? branchIds
                    : new List<int>();

                // No rows, or rows for every active branch ⇒ tenant-level (all locations).
                var isTenantLevel = !assigned.Any()
                    || (activeBranchIds.Count > 0
                        && activeBranchIds.All(id => assigned.Contains(id)));

                item.IsShared = isTenantLevel;
                item.BranchIds = isTenantLevel ? new List<int>() : assigned;
            }
        }

        private async Task OverlayBranchStockAsync(IReadOnlyList<ProductDto> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var branchId = await _branchAccessChecker.GetEffectiveBranchIdAsync();
            if (!branchId.HasValue)
            {
                return;
            }

            var infoMap = await _branchStockManager.GetBranchProductInfoAsync(
                branchId.Value,
                items.Select(x => x.Id));

            foreach (var item in items)
            {
                if (infoMap.TryGetValue(item.Id, out var info))
                {
                    item.StockQuantity = info.Quantity;
                    item.Price = info.Price;
                    item.WholesalePrice = info.WholesalePrice;
                    item.CostPrice = info.CostPrice;
                }
                else
                {
                    item.StockQuantity = 0;
                }

                item.StockProfit = ProductPricing.StockProfit(item.Price, item.CostPrice, item.StockQuantity);
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
