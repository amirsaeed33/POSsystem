using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
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
        private readonly IRepository<SmartPos.Categories.Category> _categoryRepository;
        private readonly IRepository<SmartPos.Brands.Brand> _brandRepository;
        private readonly IRepository<SmartPos.Units.Unit> _unitRepository;
        private readonly IPermissionChecker _permissionChecker;
        private int? _effectiveBranchIdForQuery;

        public ProductAppService(
            IRepository<Product> repository,
            IBranchAccessChecker branchAccessChecker,
            IBranchStockManager branchStockManager,
            IRepository<BranchStock> branchStockRepository,
            IRepository<Branch> branchRepository,
            IRepository<SmartPos.Categories.Category> categoryRepository,
            IRepository<SmartPos.Brands.Brand> brandRepository,
            IRepository<SmartPos.Units.Unit> unitRepository,
            IPermissionChecker permissionChecker)
            : base(repository)
        {
            _branchAccessChecker = branchAccessChecker;
            _branchStockManager = branchStockManager;
            _branchStockRepository = branchStockRepository;
            _branchRepository = branchRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _unitRepository = unitRepository;
            _permissionChecker = permissionChecker;
            CreatePermissionName = PermissionNames.Pages_Products_Create;
            UpdatePermissionName = PermissionNames.Pages_Products_Edit;
            DeletePermissionName = PermissionNames.Pages_Products_Delete;
            GetPermissionName = PermissionNames.Pages_Products;
            GetAllPermissionName = PermissionNames.Pages_Products;
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
            product.ImagePath = ProductImageStore.SaveBase64Image(input.ImageBase64);

            int resolvedBranchId = input.BranchId;
            if (resolvedBranchId <= 0)
            {
                var effectiveBranchId = await _branchAccessChecker.GetEffectiveBranchIdAsync();
                if (effectiveBranchId.HasValue && effectiveBranchId.Value > 0)
                {
                    resolvedBranchId = effectiveBranchId.Value;
                }
                else if (targetBranchIds != null && targetBranchIds.Any())
                {
                    resolvedBranchId = targetBranchIds.First();
                }
                else
                {
                    var defaultBranch = await _branchRepository.FirstOrDefaultAsync(b => b.IsActive);
                    resolvedBranchId = defaultBranch?.Id ?? (await _branchRepository.GetAll().Select(b => b.Id).FirstOrDefaultAsync());
                }
            }

            if (resolvedBranchId <= 0)
            {
                throw new UserFriendlyException("Please select or create a valid store branch before creating products.");
            }

            product.BranchId = resolvedBranchId;
            if (!targetBranchIds.Contains(resolvedBranchId))
            {
                targetBranchIds.Add(resolvedBranchId);
            }

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

            if (input.BranchId > 0)
            {
                product.BranchId = input.BranchId;
            }
            else if (product.BranchId <= 0)
            {
                var effectiveBranchId = await _branchAccessChecker.GetEffectiveBranchIdAsync();
                if (effectiveBranchId.HasValue && effectiveBranchId.Value > 0)
                {
                    product.BranchId = effectiveBranchId.Value;
                }
            }

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
                return query;
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
                var currentBranchId = await _branchAccessChecker.GetEffectiveBranchIdAsync();
                if (currentBranchId.HasValue)
                {
                    selected.Add(currentBranchId.Value);
                }
            }

            if (selected.Any())
            {
                await EnsureBranchesExistAndAccessibleAsync(selected);
            }

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
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                return await _branchRepository.GetAll()
                    .Where(x => x.IsActive && x.TenantId != null)
                    .Select(x => x.Id)
                    .ToListAsync();
            }
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

        public async Task<ProductImportResultDto> ImportProductsAsync(List<ImportProductRowDto> inputs)
        {
            CheckCreatePermission();

            var result = new ProductImportResultDto();
            if (inputs == null || inputs.Count == 0)
            {
                result.ErrorMessages.Add("No product rows found in the uploaded file.");
                return result;
            }

            var activeBranchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();

            // Cache or find Categories, Brands, Units
            var categories = await _categoryRepository.GetAllListAsync();
            var brands = await _brandRepository.GetAllListAsync();
            var units = await _unitRepository.GetAllListAsync();

            // Default fallback or matching category
            var defaultCategory = categories.FirstOrDefault() ?? await _categoryRepository.InsertAsync(new SmartPos.Categories.Category { Name = "General", BranchId = activeBranchId });
            var defaultBrand = brands.FirstOrDefault() ?? await _brandRepository.InsertAsync(new SmartPos.Brands.Brand { Name = "General", BranchId = activeBranchId });
            var defaultUnit = units.FirstOrDefault() ?? await _unitRepository.InsertAsync(new SmartPos.Units.Unit { Name = "Pcs", Symbol = "pcs", BranchId = activeBranchId });

            int rowIndex = 1;
            foreach (var row in inputs)
            {
                rowIndex++;
                try
                {
                    if (row.Name.IsNullOrWhiteSpace())
                    {
                        result.ErrorCount++;
                        result.ErrorMessages.Add($"Row {rowIndex}: Product name is required.");
                        continue;
                    }

                    if (row.Barcode.IsNullOrWhiteSpace())
                    {
                        result.ErrorCount++;
                        result.ErrorMessages.Add($"Row {rowIndex}: Barcode is required for product '{row.Name}'.");
                        continue;
                    }

                    var cleanBarcode = row.Barcode.Trim();
                    var existingWithBarcode = await Repository.FirstOrDefaultAsync(x => x.Barcode == cleanBarcode);
                    if (existingWithBarcode != null)
                    {
                        result.ErrorCount++;
                        result.ErrorMessages.Add($"Row {rowIndex}: Barcode '{cleanBarcode}' already exists for product '{existingWithBarcode.Name}'.");
                        continue;
                    }

                    // Resolve Category
                    var categoryId = defaultCategory.Id;
                    if (!row.CategoryName.IsNullOrWhiteSpace())
                    {
                        var cat = categories.FirstOrDefault(c => c.Name.Equals(row.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (cat != null)
                        {
                            categoryId = cat.Id;
                        }
                        else
                        {
                            var newCat = await _categoryRepository.InsertAsync(new SmartPos.Categories.Category { Name = row.CategoryName.Trim(), BranchId = activeBranchId });
                            await CurrentUnitOfWork.SaveChangesAsync();
                            categories.Add(newCat);
                            categoryId = newCat.Id;
                        }
                    }

                    // Resolve Brand
                    var brandId = defaultBrand.Id;
                    if (!row.BrandName.IsNullOrWhiteSpace())
                    {
                        var bnd = brands.FirstOrDefault(b => b.Name.Equals(row.BrandName.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (bnd != null)
                        {
                            brandId = bnd.Id;
                        }
                        else
                        {
                            var newBnd = await _brandRepository.InsertAsync(new SmartPos.Brands.Brand { Name = row.BrandName.Trim(), BranchId = activeBranchId });
                            await CurrentUnitOfWork.SaveChangesAsync();
                            brands.Add(newBnd);
                            brandId = newBnd.Id;
                        }
                    }

                    // Resolve Unit
                    var unitId = defaultUnit.Id;
                    if (!row.UnitName.IsNullOrWhiteSpace())
                    {
                        var u = units.FirstOrDefault(uItem => uItem.Name.Equals(row.UnitName.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (u != null)
                        {
                            unitId = u.Id;
                        }
                        else
                        {
                            var newU = await _unitRepository.InsertAsync(new SmartPos.Units.Unit { Name = row.UnitName.Trim(), Symbol = row.UnitName.Trim(), BranchId = activeBranchId });
                            await CurrentUnitOfWork.SaveChangesAsync();
                            units.Add(newU);
                            unitId = newU.Id;
                        }
                    }

                    var createDto = new CreateProductDto
                    {
                        Name = row.Name.Trim(),
                        Barcode = cleanBarcode,
                        Price = row.Price > 0 ? row.Price : 1,
                        WholesalePrice = row.WholesalePrice >= 0 ? row.WholesalePrice : 0,
                        CostPrice = row.CostPrice >= 0 ? row.CostPrice : 0,
                        StockQuantity = row.StockQuantity >= 0 ? row.StockQuantity : 0,
                        AlertQuantityLimit = row.AlertQuantityLimit >= 0 ? row.AlertQuantityLimit : 10,
                        CategoryId = categoryId,
                        BrandId = brandId,
                        UnitId = unitId,
                        BranchId = activeBranchId,
                        Location = row.Location?.Trim(),
                        Description = row.Description?.Trim()
                    };

                    await CreateAsync(createDto);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.ErrorMessages.Add($"Row {rowIndex} ({row.Name}): {ex.Message}");
                }
            }

            return result;
        }
    }
}
