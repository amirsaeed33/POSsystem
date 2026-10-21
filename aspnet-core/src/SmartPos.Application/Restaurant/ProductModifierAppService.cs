using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Restaurant.Dto;

namespace SmartPos.Restaurant
{
    [AbpAuthorize]
    public class ProductModifierAppService : ApplicationService, IProductModifierAppService
    {
        private readonly IRepository<ProductModifierGroup> _groupRepository;
        private readonly IRepository<ProductModifierOption> _optionRepository;

        public ProductModifierAppService(
            IRepository<ProductModifierGroup> groupRepository,
            IRepository<ProductModifierOption> optionRepository)
        {
            _groupRepository = groupRepository;
            _optionRepository = optionRepository;
        }

        public async Task<List<ProductModifierGroupDto>> GetByProductAsync(int productId)
        {
            var groups = await _groupRepository.GetAll()
                .Include(g => g.Options)
                .Where(g => g.ProductId == productId)
                .OrderBy(g => g.Name)
                .ToListAsync();

            return ObjectMapper.Map<List<ProductModifierGroupDto>>(groups);
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

            return ObjectMapper.Map<ProductModifierGroupDto>(group);
        }

        public async Task DeleteGroupAsync(EntityDto<int> input)
        {
            await _groupRepository.DeleteAsync(input.Id);
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

            return ObjectMapper.Map<ProductModifierOptionDto>(option);
        }

        public async Task DeleteOptionAsync(EntityDto<int> input)
        {
            await _optionRepository.DeleteAsync(input.Id);
        }
    }
}
