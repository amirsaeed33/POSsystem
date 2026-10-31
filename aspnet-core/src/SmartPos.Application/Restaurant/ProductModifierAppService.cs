using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Caching;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Caching;
using SmartPos.Restaurant.Dto;

namespace SmartPos.Restaurant
{
    [AbpAuthorize]
    public class ProductModifierAppService : ApplicationService, IProductModifierAppService
    {
        private readonly IRepository<ProductModifierGroup> _groupRepository;
        private readonly IRepository<ProductModifierOption> _optionRepository;
        private readonly ICacheManager _cacheManager;
        private readonly ISmartPosReadCacheInvalidator _readCacheInvalidator;

        public ProductModifierAppService(
            IRepository<ProductModifierGroup> groupRepository,
            IRepository<ProductModifierOption> optionRepository,
            ICacheManager cacheManager,
            ISmartPosReadCacheInvalidator readCacheInvalidator)
        {
            _groupRepository = groupRepository;
            _optionRepository = optionRepository;
            _cacheManager = cacheManager;
            _readCacheInvalidator = readCacheInvalidator;
        }

        public async Task<List<ProductModifierGroupDto>> GetByProductAsync(int productId)
        {
            var cacheKey = SmartPosAppCaches.ProductKey(productId);
            var cache = SmartPosAppCaches.Typed<List<ProductModifierGroupDto>>(
                _cacheManager,
                SmartPosAppCaches.ProductModifiers,
                TimeSpan.FromMinutes(15));

            return await cache.GetAsync(cacheKey, async _ =>
            {
                var groups = await _groupRepository.GetAll()
                    .AsNoTracking()
                    .Include(g => g.Options)
                    .Where(g => g.ProductId == productId)
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                return ObjectMapper.Map<List<ProductModifierGroupDto>>(groups);
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
                IsDefault = input.IsDefault
            };

            var id = await _optionRepository.InsertAndGetIdAsync(option);
            option.Id = id;
            await _readCacheInvalidator.ClearModifiersAsync();

            return ObjectMapper.Map<ProductModifierOptionDto>(option);
        }

        public async Task DeleteOptionAsync(EntityDto<int> input)
        {
            await _optionRepository.DeleteAsync(input.Id);
            await _readCacheInvalidator.ClearModifiersAsync();
        }
    }
}
