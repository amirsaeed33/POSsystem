using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Caching;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Branches;
using SmartPos.Brands;
using SmartPos.Caching;
using SmartPos.Categories;
using SmartPos.Deals.Dto;
using SmartPos.Inventory;
using SmartPos.Products;
using SmartPos.Units;

namespace SmartPos.Deals
{
    [AbpAuthorize(PermissionNames.Pages_Products)]
    public class DealAppService : ApplicationService, IDealAppService
    {
        private readonly IRepository<Deal> _dealRepository;
        private readonly IRepository<DealDetail> _dealDetailRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Unit> _unitRepository;
        private readonly IRepository<Brand> _brandRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchStockManager _branchStockManager;
        private readonly ICacheManager _cacheManager;
        private readonly ISmartPosReadCacheInvalidator _readCacheInvalidator;

        public DealAppService(
            IRepository<Deal> dealRepository,
            IRepository<DealDetail> dealDetailRepository,
            IRepository<Product> productRepository,
            IRepository<Category> categoryRepository,
            IRepository<Unit> unitRepository,
            IRepository<Brand> brandRepository,
            IRepository<Branch> branchRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchStockManager branchStockManager,
            ICacheManager cacheManager,
            ISmartPosReadCacheInvalidator readCacheInvalidator)
        {
            _dealRepository = dealRepository;
            _dealDetailRepository = dealDetailRepository;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _unitRepository = unitRepository;
            _brandRepository = brandRepository;
            _branchRepository = branchRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchStockManager = branchStockManager;
            _cacheManager = cacheManager;
            _readCacheInvalidator = readCacheInvalidator;
        }

        public async Task<DealDto> CreateAsync(CreateDealDto input)
        {
            try
            {
                var branchId = input.BranchId ?? await _branchAccessChecker.RequireEffectiveBranchIdAsync();
                var branch = await _branchRepository.GetAsync(branchId);
                if (branch.BusinessType != BusinessType.Restaurant)
                {
                    throw new UserFriendlyException("Deals feature is strictly available for Restaurant branches only.");
                }

                if (string.IsNullOrWhiteSpace(input.Name))
                {
                    throw new UserFriendlyException("Deal name is required.");
                }

                if (input.DealPrice <= 0)
                {
                    throw new UserFriendlyException("Deal price must be greater than zero.");
                }

                if (input.Items == null || input.Items.Count == 0)
                {
                    throw new UserFriendlyException("Please select at least one product for the deal.");
                }

                var productIds = input.Items.Select(x => x.ProductId).ToList();
                if (productIds.Distinct().Count() != productIds.Count)
                {
                    throw new UserFriendlyException("Each product can only be added once to a deal. Adjust the quantity instead.");
                }

                var products = await _productRepository.GetAllListAsync(p => productIds.Contains(p.Id));
                if (products.Count != productIds.Count)
                {
                    throw new UserFriendlyException("One or more selected products could not be found.");
                }

                // 1. Ensure Deals Category exists
                Category dealsCategory = null;
                if (input.CategoryId.HasValue && input.CategoryId.Value > 0)
                {
                    dealsCategory = await _categoryRepository.FirstOrDefaultAsync(input.CategoryId.Value);
                }

                if (dealsCategory == null)
                {
                    dealsCategory = await _categoryRepository.FirstOrDefaultAsync(c =>
                        c.BranchId == branchId && (c.Name == "Deals" || c.Name == "Deal"));
                }

                if (dealsCategory == null)
                {
                    dealsCategory = await _categoryRepository.FirstOrDefaultAsync(c =>
                        c.Name == "Deals" || c.Name == "Deal");
                }

                if (dealsCategory == null)
                {
                    var defaultCatUnit = await _unitRepository.FirstOrDefaultAsync(u => u.Name == "Piece")
                                          ?? await _unitRepository.FirstOrDefaultAsync(u => u.Id > 0);
                    dealsCategory = await _categoryRepository.InsertAsync(new Category
                    {
                        TenantId = AbpSession.TenantId,
                        BranchId = branchId,
                        Name = "Deals",
                        Description = "Deals",
                        DefaultUnitId = defaultCatUnit?.Id
                    });
                    await CurrentUnitOfWork.SaveChangesAsync();
                }

                // 2. Resolve default Unit
                var unit = await _unitRepository.FirstOrDefaultAsync(u => u.Name == "Piece")
                           ?? await _unitRepository.FirstOrDefaultAsync(u => u.BranchId == branchId)
                           ?? await _unitRepository.FirstOrDefaultAsync(u => u.Id > 0);
                int unitId = unit?.Id ?? 1;

                // 3. Resolve Brand (Product requires non-nullable BrandId)
                var defaultBrand = (branch != null ? await _brandRepository.FirstOrDefaultAsync(b => b.BranchId == branch.Id) : null)
                                   ?? await _brandRepository.FirstOrDefaultAsync(b => true);
                if (defaultBrand == null)
                {
                    using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                    {
                        defaultBrand = await _brandRepository.FirstOrDefaultAsync(b => true);
                    }
                }
                int brandId = defaultBrand?.Id ?? await _brandRepository.InsertAndGetIdAsync(new Brand
                {
                    TenantId = branch?.TenantId ?? AbpSession.TenantId,
                    BranchId = branch?.Id ?? 0,
                    Name = "General",
                    IsActive = true
                });

                // 4. Create Product for the Deal so it naturally works with POS and Catalog
                var product = new Product
                {
                    TenantId = AbpSession.TenantId,
                    BranchId = branchId,
                    Name = input.Name.Trim(),
                    Description = input.Description,
                    Price = input.DealPrice,
                    WholesalePrice = input.DealPrice,
                    CostPrice = input.Items.Sum(item =>
                    {
                        var p = products.FirstOrDefault(x => x.Id == item.ProductId);
                        var price = item.UnitPrice > 0 ? item.UnitPrice : (p?.CostPrice > 0 ? p.CostPrice : p?.Price ?? 0);
                        return price * item.Quantity;
                    }),
                    CategoryId = dealsCategory.Id,
                    BrandId = brandId,
                    UnitId = unitId,
                    Barcode = $"DEAL-{Guid.NewGuid():N}".Substring(0, 16).ToUpper(),
                    IsDeal = true,
                    AlertQuantityLimit = 0,
                    ImagePath = ProductImageStore.SaveBase64Image(input.ImageBase64)
                };
                var createdProduct = await _productRepository.InsertAsync(product);
                await CurrentUnitOfWork.SaveChangesAsync();

                // Set stock row so it appears in POS
                await _branchStockManager.UpsertStockAndPricesAsync(
                    branchId,
                    createdProduct.Id,
                    9999,
                    createdProduct.Price,
                    createdProduct.WholesalePrice,
                    createdProduct.CostPrice);

                // 5. Create Deal Entity
                var deal = new Deal
                {
                    TenantId = AbpSession.TenantId,
                    BranchId = branchId,
                    Name = input.Name.Trim(),
                    Description = input.Description,
                    DealPrice = input.DealPrice,
                    ExpiryDate = input.ExpiryDate,
                    CategoryType = string.IsNullOrWhiteSpace(input.CategoryType) ? "Deals" : input.CategoryType.Trim(),
                    CategoryId = dealsCategory.Id,
                    IsActive = true,
                    ProductId = createdProduct.Id
                };
                var createdDeal = await _dealRepository.InsertAsync(deal);
                await CurrentUnitOfWork.SaveChangesAsync();

                // Link DealId on product
                createdProduct.DealId = createdDeal.Id;
                await _productRepository.UpdateAsync(createdProduct);

                // 6. Create DealDetails
                foreach (var item in input.Items)
                {
                    var prod = products.First(x => x.Id == item.ProductId);
                    var unitPrice = item.UnitPrice > 0 ? item.UnitPrice : prod.Price;
                    await _dealDetailRepository.InsertAsync(new DealDetail
                    {
                        TenantId = AbpSession.TenantId,
                        DealId = createdDeal.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity > 0 ? item.Quantity : 1,
                        UnitPrice = unitPrice
                    });
                }
                await CurrentUnitOfWork.SaveChangesAsync();
                await _readCacheInvalidator.ClearCatalogAsync();

                return await GetAsync(new EntityDto<int>(createdDeal.Id));
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error("Error creating deal: " + ex);
                throw new UserFriendlyException("Failed to save deal: " + ex.Message);
            }
        }

        public async Task<DealDto> UpdateAsync(UpdateDealDto input)
        {
            try
            {
                var deal = await _dealRepository.GetAllIncluding(d => d.Product)
                    .FirstOrDefaultAsync(d => d.Id == input.Id);
                if (deal == null)
                {
                    throw new UserFriendlyException("Deal not found.");
                }

                deal.Name = input.Name.Trim();
                deal.Description = input.Description;
                deal.DealPrice = input.DealPrice;
                deal.ExpiryDate = input.ExpiryDate;
                if (!string.IsNullOrWhiteSpace(input.CategoryType))
                {
                    deal.CategoryType = input.CategoryType.Trim();
                }

                // Sync linked Product
                Product product = null;
                if (deal.ProductId.HasValue)
                {
                    product = await _productRepository.FirstOrDefaultAsync(deal.ProductId.Value);
                }
                if (product == null)
                {
                    product = await _productRepository.FirstOrDefaultAsync(p => p.DealId == deal.Id);
                }

                if (product != null)
                {
                    product.Name = input.Name.Trim();
                    product.Price = input.DealPrice;
                    product.WholesalePrice = input.DealPrice;
                    product.Description = input.Description;
                    product.IsDeal = true;
                    product.DealId = deal.Id;
                    if (ProductImageStore.IsNewImagePayload(input.ImageBase64))
                    {
                        ProductImageStore.DeleteIfExists(product.ImagePath);
                        product.ImagePath = ProductImageStore.SaveBase64Image(input.ImageBase64);
                    }
                    await _productRepository.UpdateAsync(product);
                }

                await _dealRepository.UpdateAsync(deal);

                // Update DealDetails
                var existingDetails = await _dealDetailRepository.GetAllListAsync(dd => dd.DealId == deal.Id);
                foreach (var detail in existingDetails)
                {
                    await _dealDetailRepository.DeleteAsync(detail);
                }
                await CurrentUnitOfWork.SaveChangesAsync();

                if (input.Items != null && input.Items.Count > 0)
                {
                    var productIds = input.Items.Select(x => x.ProductId).Distinct().ToList();
                    var products = await _productRepository.GetAllListAsync(p => productIds.Contains(p.Id));

                    foreach (var item in input.Items)
                    {
                        var prod = products.FirstOrDefault(x => x.Id == item.ProductId);
                        var unitPrice = item.UnitPrice > 0
                            ? item.UnitPrice
                            : (prod?.Price ?? 0);

                        await _dealDetailRepository.InsertAsync(new DealDetail
                        {
                            TenantId = AbpSession.TenantId,
                            DealId = deal.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity > 0 ? item.Quantity : 1,
                            UnitPrice = unitPrice
                        });
                    }
                    await CurrentUnitOfWork.SaveChangesAsync();
                }

                await _readCacheInvalidator.ClearCatalogAsync();
                return await GetAsync(new EntityDto<int>(deal.Id));
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating deal: " + ex);
                throw new UserFriendlyException("Failed to update deal: " + ex.Message);
            }
        }

        public async Task<DealDto> GetAsync(EntityDto<int> input)
        {
            var deal = await _dealRepository.GetAllIncluding(d => d.Branch, d => d.Category, d => d.Product)
                .FirstOrDefaultAsync(d => d.Id == input.Id);
            if (deal == null)
            {
                throw new UserFriendlyException("Deal not found.");
            }

            var details = await _dealDetailRepository.GetAllIncluding(dd => dd.Product)
                .Where(dd => dd.DealId == deal.Id)
                .ToListAsync();

            return MapToDealDto(deal, details);
        }

        public async Task<DealDto> GetByProductIdAsync(int productId)
        {
            var cacheKey = SmartPosAppCaches.ProductKey(productId);
            var cache = SmartPosAppCaches.Typed<DealDto>(
                _cacheManager,
                SmartPosAppCaches.DealByProduct,
                TimeSpan.FromMinutes(10));

            return await cache.GetAsync(cacheKey, async _ =>
            {
                var deal = await _dealRepository.GetAllIncluding(d => d.Branch, d => d.Category, d => d.Product)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.ProductId == productId);
                if (deal == null)
                {
                    var prod = await _productRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
                    if (prod?.DealId != null)
                    {
                        deal = await _dealRepository.GetAllIncluding(d => d.Branch, d => d.Category, d => d.Product)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(d => d.Id == prod.DealId.Value);
                    }
                }

                if (deal == null)
                {
                    throw new UserFriendlyException("Deal details not found for this product.");
                }

                var details = await _dealDetailRepository.GetAllIncluding(dd => dd.Product)
                    .AsNoTracking()
                    .Where(dd => dd.DealId == deal.Id)
                    .ToListAsync();

                return MapToDealDto(deal, details);
            });
        }

        public async Task<PagedResultDto<DealDto>> GetAllAsync(PagedDealResultRequestDto input)
        {
            var branchId = input.BranchId ?? await _branchAccessChecker.GetEffectiveBranchIdAsync();
            var query = _dealRepository.GetAllIncluding(d => d.Branch, d => d.Category, d => d.Product);

            if (branchId.HasValue)
            {
                query = query.Where(d => d.BranchId == branchId.Value);
            }

            if (!string.IsNullOrWhiteSpace(input.Keyword))
            {
                var keyword = input.Keyword.Trim().ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(keyword) || (d.Description != null && d.Description.ToLower().Contains(keyword)));
            }

            var totalCount = await query.CountAsync();
            var deals = await query.OrderByDescending(d => d.Id)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToListAsync();

            var dealIds = deals.Select(d => d.Id).ToList();
            var allDetails = await _dealDetailRepository.GetAllIncluding(dd => dd.Product)
                .Where(dd => dealIds.Contains(dd.DealId))
                .ToListAsync();

            var dtos = deals.Select(d =>
            {
                var details = allDetails.Where(dd => dd.DealId == d.Id).ToList();
                return MapToDealDto(d, details);
            }).ToList();

            return new PagedResultDto<DealDto>(totalCount, dtos);
        }

        public async Task DeleteAsync(EntityDto<int> input)
        {
            var deal = await _dealRepository.FirstOrDefaultAsync(input.Id);
            if (deal != null)
            {
                if (deal.ProductId.HasValue)
                {
                    await _productRepository.DeleteAsync(deal.ProductId.Value);
                }
                var details = await _dealDetailRepository.GetAllListAsync(dd => dd.DealId == deal.Id);
                foreach (var detail in details)
                {
                    await _dealDetailRepository.DeleteAsync(detail);
                }
                await _dealRepository.DeleteAsync(deal);
                await _readCacheInvalidator.ClearCatalogAsync();
            }
        }

        private static DealDto MapToDealDto(Deal deal, List<DealDetail> details)
        {
            return new DealDto
            {
                Id = deal.Id,
                BranchId = deal.BranchId,
                BranchName = deal.Branch?.Name,
                Name = deal.Name,
                Description = deal.Description,
                DealPrice = deal.DealPrice,
                ExpiryDate = deal.ExpiryDate,
                CategoryType = deal.CategoryType,
                CategoryId = deal.CategoryId,
                CategoryName = deal.Category?.Name ?? "Deals",
                IsActive = deal.IsActive,
                ProductId = deal.ProductId,
                ImagePath = deal.Product?.ImagePath,
                Items = details.Select(dd => new DealDetailDto
                {
                    Id = dd.Id,
                    DealId = dd.DealId,
                    ProductId = dd.ProductId,
                    ProductName = dd.Product?.Name ?? "Product #" + dd.ProductId,
                    ProductBarcode = dd.Product?.Barcode,
                    Quantity = dd.Quantity,
                    UnitPrice = dd.UnitPrice
                }).ToList()
            };
        }
    }
}
