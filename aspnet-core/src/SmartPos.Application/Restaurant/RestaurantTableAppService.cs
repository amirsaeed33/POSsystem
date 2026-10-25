using System;
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
    public class RestaurantTableAppService : ApplicationService, IRestaurantTableAppService
    {
        private readonly IRepository<RestaurantTable> _tableRepository;
        private readonly IRepository<RestaurantOrder> _orderRepository;

        public RestaurantTableAppService(
            IRepository<RestaurantTable> tableRepository,
            IRepository<RestaurantOrder> orderRepository)
        {
            _tableRepository = tableRepository;
            _orderRepository = orderRepository;
        }

        public async Task<List<RestaurantTableDto>> GetTablesAsync(int branchId)
        {
            var tables = await _tableRepository.GetAll()
                .Where(t => t.BranchId == branchId && t.IsActive)
                .OrderBy(t => t.Area)
                .ThenBy(t => t.TableNo)
                .ToListAsync();

            return ObjectMapper.Map<List<RestaurantTableDto>>(tables);
        }

        public async Task<RestaurantTableDto> GetAsync(EntityDto<int> input)
        {
            var table = await _tableRepository.GetAsync(input.Id);
            return ObjectMapper.Map<RestaurantTableDto>(table);
        }

        public async Task<RestaurantTableDto> CreateAsync(CreateRestaurantTableDto input)
        {
            var exists = await _tableRepository.GetAll()
                .AnyAsync(t => t.BranchId == input.BranchId && t.TableNo.ToLower() == input.TableNo.Trim().ToLower());

            if (exists)
            {
                throw new UserFriendlyException($"Table '{input.TableNo}' already exists in this branch.");
            }

            var table = ObjectMapper.Map<RestaurantTable>(input);
            table.Status = TableStatus.Available;
            table.TableNo = input.TableNo.Trim();
            table.Area = string.IsNullOrWhiteSpace(input.Area) ? "Main Hall" : input.Area.Trim();

            var id = await _tableRepository.InsertAndGetIdAsync(table);
            table.Id = id;

            return ObjectMapper.Map<RestaurantTableDto>(table);
        }

        public async Task<RestaurantTableDto> UpdateAsync(RestaurantTableDto input)
        {
            var table = await _tableRepository.GetAsync(input.Id);

            var duplicate = await _tableRepository.GetAll()
                .AnyAsync(t => t.BranchId == table.BranchId && t.Id != input.Id && t.TableNo.ToLower() == input.TableNo.Trim().ToLower());

            if (duplicate)
            {
                throw new UserFriendlyException($"Another table with name '{input.TableNo}' already exists in this branch.");
            }

            table.TableNo = input.TableNo.Trim();
            table.Area = string.IsNullOrWhiteSpace(input.Area) ? "Main Hall" : input.Area.Trim();
            table.Capacity = input.Capacity;
            table.IsActive = input.IsActive;

            await _tableRepository.UpdateAsync(table);

            return ObjectMapper.Map<RestaurantTableDto>(table);
        }

        public async Task DeleteAsync(EntityDto<int> input)
        {
            var table = await _tableRepository.GetAsync(input.Id);
            if (table.Status != TableStatus.Available)
            {
                throw new UserFriendlyException("Cannot delete an occupied or billed table.");
            }

            await _tableRepository.DeleteAsync(table);
        }

        public async Task<RestaurantTableDto> ChangeStatusAsync(ChangeTableStatusDto input)
        {
            var table = await _tableRepository.GetAsync(input.TableId);
            table.Status = input.Status;
            table.CurrentOrderId = input.CurrentOrderId;

            await _tableRepository.UpdateAsync(table);
            return ObjectMapper.Map<RestaurantTableDto>(table);
        }

        public async Task TransferTableAsync(int fromTableId, int toTableId)
        {
            if (fromTableId == toTableId)
            {
                throw new UserFriendlyException("Source and destination tables must be different.");
            }

            var source = await _tableRepository.GetAsync(fromTableId);
            var dest = await _tableRepository.GetAsync(toTableId);

            if (dest.Status != TableStatus.Available)
            {
                throw new UserFriendlyException($"Destination table '{dest.TableNo}' is not available.");
            }

            dest.Status = source.Status;
            dest.CurrentOrderId = source.CurrentOrderId;
            dest.WaiterId = source.WaiterId;
            dest.WaiterName = source.WaiterName;

            if (source.CurrentOrderId.HasValue)
            {
                var order = await _orderRepository.FirstOrDefaultAsync(source.CurrentOrderId.Value);
                if (order != null)
                {
                    order.TableId = dest.Id;
                    order.TableNo = dest.TableNo;
                    if (!string.IsNullOrWhiteSpace(dest.WaiterName))
                    {
                        order.WaiterName = dest.WaiterName;
                    }
                    await _orderRepository.UpdateAsync(order);
                }
            }

            source.Status = TableStatus.Available;
            source.CurrentOrderId = null;
            source.WaiterId = null;
            source.WaiterName = null;

            await _tableRepository.UpdateAsync(source);
            await _tableRepository.UpdateAsync(dest);
        }

        public async Task<RestaurantTableDto> AssignWaiterAsync(AssignWaiterDto input)
        {
            var table = await _tableRepository.GetAsync(input.TableId);
            table.WaiterId = input.WaiterId;
            table.WaiterName = input.WaiterName?.Trim();
            await _tableRepository.UpdateAsync(table);

            if (table.CurrentOrderId.HasValue)
            {
                var order = await _orderRepository.FirstOrDefaultAsync(table.CurrentOrderId.Value);
                if (order != null)
                {
                    order.WaiterName = table.WaiterName;
                    await _orderRepository.UpdateAsync(order);
                }
            }

            return ObjectMapper.Map<RestaurantTableDto>(table);
        }
    }
}
