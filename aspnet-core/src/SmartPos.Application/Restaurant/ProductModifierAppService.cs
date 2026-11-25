using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Caching;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Branches;
using SmartPos.Caching;
using SmartPos.Products;
using SmartPos.Restaurant.Dto;

namespace SmartPos.Restaurant
{
    [AbpAuthorize]
    public class ProductModifierAppService : ApplicationService, IProductModifierAppService
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly IRepository<ProductModifierGroup> _groupRepository;
        private readonly IRepository<ProductModifierOption> _optionRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly ICacheManager _cacheManager;
        private readonly ISmartPosReadCacheInvalidator _readCacheInvalidator;

        public ProductModifierAppService(
            IRepository<ProductModifierGroup> groupRepository,
            IRepository<ProductModifierOption> optionRepository,
            IRepository<Product> productRepository,
            IRepository<Branch> branchRepository,
            ICacheManager cacheManager,
            ISmartPosReadCacheInvalidator readCacheInvalidator)
        {
            _groupRepository = groupRepository;
            _optionRepository = optionRepository;
            _productRepository = productRepository;
            _branchRepository = branchRepository;
            _cacheManager = cacheManager;
            _readCacheInvalidator = readCacheInvalidator;
        }

        [AbpAllowAnonymous]
        public async Task<List<ProductModifierGroupDto>> GetByProductAsync(int productId)
        {
            var cacheKey = SmartPosAppCaches.ProductKey(productId);
            var cache = SmartPosAppCaches.Typed<List<ProductModifierGroupDto>>(
                _cacheManager,
                SmartPosAppCaches.ProductModifiers,
                TimeSpan.FromMinutes(15));

            return await cache.GetAsync(cacheKey, async _ =>
            {
                // Online storefront is often anonymous / cross-tenant by branch — same as catalog.
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
                {
                    var groups = await _groupRepository.GetAll()
                        .AsNoTracking()
                        .Include(g => g.Options)
                        .Where(g => g.ProductId == productId)
                        .OrderBy(g => g.Name)
                        .ToListAsync();

                    return ObjectMapper.Map<List<ProductModifierGroupDto>>(groups);
                }
            });
        }

        public async Task<ProductModifierGroupDto> CreateGroupAsync(CreateModifierGroupDto input)
        {
            var group = new ProductModifierGroup
            {
                ProductId = input.ProductId,
                Name = input.Name.Trim(),
                MinSelection = input.MinSelection,
                MaxSelection = input.MaxSelection,
                IsRequired = input.IsRequired,
                Options = (input.Options ?? new List<CreateModifierOptionDto>()).Select(o => new ProductModifierOption
                {
                    Name = o.Name.Trim(),
                    Price = o.Price,
                    IsIncludedInBasePrice = o.IsIncludedInBasePrice,
                    IsDefault = o.IsDefault
                }).ToList()
            };

            var id = await _groupRepository.InsertAndGetIdAsync(group);
            group.Id = id;
            await _readCacheInvalidator.ClearModifiersAsync();

            return ObjectMapper.Map<ProductModifierGroupDto>(group);
        }

        public async Task DeleteGroupAsync(EntityDto<int> input)
        {
            await _groupRepository.DeleteAsync(input.Id);
            await _readCacheInvalidator.ClearModifiersAsync();
        }

        public async Task<ProductModifierOptionDto> AddOptionAsync(int groupId, CreateModifierOptionDto input)
        {
            var group = await _groupRepository.GetAsync(groupId);
            var option = new ProductModifierOption
            {
                ModifierGroupId = groupId,
                Name = input.Name.Trim(),
                Price = input.Price,
                IsIncludedInBasePrice = input.IsIncludedInBasePrice,
                IsDefault = input.IsDefault
            };

            var id = await _optionRepository.InsertAndGetIdAsync(option);
            option.Id = id;
            await _readCacheInvalidator.ClearModifiersAsync();

            return ObjectMapper.Map<ProductModifierOptionDto>(option);
        }

        public async Task<ProductModifierOptionDto> UpdateOptionAsync(UpdateModifierOptionDto input)
        {
            if (input == null || input.Id <= 0)
            {
                throw new UserFriendlyException("Option is required.");
            }

            var name = (input.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new UserFriendlyException("Option name is required.");
            }

            var option = await _optionRepository.GetAsync(input.Id);
            option.Name = name;
            option.Price = input.Price < 0 ? 0 : input.Price;
            option.IsIncludedInBasePrice = input.IsIncludedInBasePrice;
            option.IsDefault = input.IsDefault;

            await _optionRepository.UpdateAsync(option);
            await CurrentUnitOfWork.SaveChangesAsync();
            await _readCacheInvalidator.ClearModifiersAsync();

            return ObjectMapper.Map<ProductModifierOptionDto>(option);
        }

        public async Task DeleteOptionAsync(EntityDto<int> input)
        {
            await _optionRepository.DeleteAsync(input.Id);
            await _readCacheInvalidator.ClearModifiersAsync();
        }

        public async Task<BranchModifierTemplateDto> GetBranchTemplateAsync(int branchId)
        {
            if (branchId <= 0)
            {
                throw new UserFriendlyException("Branch is required.");
            }

            var branch = await _branchRepository.FirstOrDefaultAsync(branchId);
            if (branch == null)
            {
                throw new UserFriendlyException("Branch not found.");
            }

            return DeserializeTemplate(branch.ModifierTemplateJson, branchId);
        }

        public async Task<BranchModifierTemplateDto> SaveBranchTemplateAsync(BranchModifierTemplateDto input)
        {
            if (input == null || input.BranchId <= 0)
            {
                throw new UserFriendlyException("Branch is required.");
            }

            var branch = await _branchRepository.FirstOrDefaultAsync(input.BranchId);
            if (branch == null)
            {
                throw new UserFriendlyException("Branch not found.");
            }

            var cleaned = NormalizeTemplate(input);
            branch.ModifierTemplateJson = JsonSerializer.Serialize(cleaned, JsonOptions);
            await _branchRepository.UpdateAsync(branch);
            await CurrentUnitOfWork.SaveChangesAsync();

            return cleaned;
        }

        public async Task<List<ProductModifierGroupDto>> ApplyBranchTemplateAsync(ApplyModifierTemplateInput input)
        {
            if (input == null || input.ProductId <= 0)
            {
                throw new UserFriendlyException("Product is required.");
            }

            var product = await _productRepository.FirstOrDefaultAsync(input.ProductId);
            if (product == null)
            {
                throw new UserFriendlyException("Product not found.");
            }

            var existing = await _groupRepository.CountAsync(g => g.ProductId == input.ProductId);
            if (existing > 0)
            {
                return await GetByProductAsync(input.ProductId);
            }

            var branchId = input.BranchId.GetValueOrDefault();
            if (branchId <= 0)
            {
                branchId = product.BranchId;
            }

            if (branchId <= 0)
            {
                return new List<ProductModifierGroupDto>();
            }

            var template = await GetBranchTemplateAsync(branchId);
            if (template?.Options == null || template.Options.Count == 0)
            {
                return new List<ProductModifierGroupDto>();
            }

            var sale = product.Price > 0 ? product.Price : 0m;
            var group = new ProductModifierGroup
            {
                ProductId = product.Id,
                Name = string.IsNullOrWhiteSpace(template.GroupName) ? "Size" : template.GroupName.Trim(),
                IsRequired = template.IsRequired,
                MinSelection = template.IsRequired ? 1 : 0,
                MaxSelection = template.AllowMultiple ? 10 : 1,
                Options = template.Options
                    .Where(o => !string.IsNullOrWhiteSpace(o.Name))
                    .Select((o, idx) =>
                    {
                        var useSale = o.UseSalePrice || idx == 0;
                        return new ProductModifierOption
                        {
                            Name = o.Name.Trim(),
                            Price = useSale ? sale : Math.Max(0, o.Price),
                            IsIncludedInBasePrice = useSale,
                            IsDefault = useSale || o.IsDefault
                        };
                    })
                    .ToList()
            };

            // Only one default.
            var defaults = group.Options.Where(x => x.IsDefault).ToList();
            if (defaults.Count > 1)
            {
                for (var i = 1; i < defaults.Count; i++)
                {
                    defaults[i].IsDefault = false;
                }
            }
            else if (defaults.Count == 0 && group.Options.Count > 0)
            {
                group.Options.First().IsDefault = true;
                group.Options.First().IsIncludedInBasePrice = true;
                group.Options.First().Price = sale;
            }

            await _groupRepository.InsertAsync(group);
            await CurrentUnitOfWork.SaveChangesAsync();
            await _readCacheInvalidator.ClearModifiersAsync();

            return await GetByProductAsync(product.Id);
        }

        private static BranchModifierTemplateDto DeserializeTemplate(string json, int branchId)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return DefaultTemplate(branchId);
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<BranchModifierTemplateDto>(json, JsonOptions);
                if (parsed == null)
                {
                    return DefaultTemplate(branchId);
                }

                parsed.BranchId = branchId;
                return NormalizeTemplate(parsed);
            }
            catch
            {
                return DefaultTemplate(branchId);
            }
        }

        private static BranchModifierTemplateDto NormalizeTemplate(BranchModifierTemplateDto input)
        {
            var options = (input.Options ?? new List<BranchModifierTemplateOptionDto>())
                .Where(o => o != null && !string.IsNullOrWhiteSpace(o.Name))
                .Select((o, idx) => new BranchModifierTemplateOptionDto
                {
                    Name = o.Name.Trim(),
                    UseSalePrice = o.UseSalePrice || idx == 0,
                    Price = o.UseSalePrice || idx == 0 ? 0 : Math.Max(0, o.Price),
                    IsDefault = o.UseSalePrice || idx == 0 || o.IsDefault
                })
                .ToList();

            if (options.Count == 0)
            {
                return DefaultTemplate(input.BranchId);
            }

            // Ensure exactly one default / sale-price option (first by convention).
            for (var i = 0; i < options.Count; i++)
            {
                if (i == 0)
                {
                    options[i].UseSalePrice = true;
                    options[i].IsDefault = true;
                    options[i].Price = 0;
                }
                else
                {
                    options[i].UseSalePrice = false;
                    options[i].IsDefault = false;
                }
            }

            return new BranchModifierTemplateDto
            {
                BranchId = input.BranchId,
                GroupName = string.IsNullOrWhiteSpace(input.GroupName) ? "Size" : input.GroupName.Trim(),
                IsRequired = input.IsRequired,
                AllowMultiple = input.AllowMultiple,
                Options = options
            };
        }

        private static BranchModifierTemplateDto DefaultTemplate(int branchId)
        {
            return new BranchModifierTemplateDto
            {
                BranchId = branchId,
                GroupName = "Size",
                IsRequired = true,
                AllowMultiple = false,
                Options = new List<BranchModifierTemplateOptionDto>
                {
                    new BranchModifierTemplateOptionDto { Name = "2 ml", UseSalePrice = true, Price = 0, IsDefault = true },
                    new BranchModifierTemplateOptionDto { Name = "4 ml", UseSalePrice = false, Price = 200, IsDefault = false },
                    new BranchModifierTemplateOptionDto { Name = "6 ml", UseSalePrice = false, Price = 400, IsDefault = false },
                    new BranchModifierTemplateOptionDto { Name = "8 ml", UseSalePrice = false, Price = 650, IsDefault = false },
                }
            };
        }
    }
}
