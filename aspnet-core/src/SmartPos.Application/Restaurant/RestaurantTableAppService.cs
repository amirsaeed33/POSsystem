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
using SmartPos.Authorization;
using SmartPos.Restaurant.Dto;

namespace SmartPos.Restaurant
{
    [AbpAuthorize(PermissionNames.Pages_DiningTables, PermissionNames.Pages_Pos, PermissionNames.Pages_Sales)]
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
                .AsNoTracking()
                .Where(t => t.BranchId == branchId && t.IsActive)
                .OrderBy(t => t.IsCarBay ? 1 : 0)
                .ThenBy(t => t.Area)
                .ThenBy(t => t.TableNo)
                .ToListAsync();

            var orderIds = tables
                .Where(t => t.CurrentOrderId.HasValue)
                .Select(t => t.CurrentOrderId.Value)
                .Distinct()
                .ToList();

            var vehicleByOrderId = new Dictionary<int, string>();
            if (orderIds.Count > 0)
            {
                try
                {
                    var carNumbers = await _orderRepository.GetAll()
                        .AsNoTracking()
                        .Where(o => orderIds.Contains(o.Id))
                        .Select(o => new { o.Id, o.CarNumber })
                        .ToListAsync();

                    foreach (var row in carNumbers)
                    {
                        if (!string.IsNullOrWhiteSpace(row.CarNumber))
                        {
                            vehicleByOrderId[row.Id] = row.CarNumber.Trim();
                        }
                    }
                }
                catch (Exception)
                {
                    // Older DBs may be missing car columns; still return tables/bays.
                }
            }

            var result = new List<RestaurantTableDto>(tables.Count);
            foreach (var table in tables)
            {
                var dto = ObjectMapper.Map<RestaurantTableDto>(table);
                dto.IsCarBay = RestaurantTable.LooksLikeCarBay(table.TableNo, table.Area, table.IsCarBay);
                if (table.CurrentOrderId.HasValue
                    && vehicleByOrderId.TryGetValue(table.CurrentOrderId.Value, out var plate))
                {
                    dto.CurrentVehicleNumber = plate;
                }
                result.Add(dto);
            }

            return result;
        }

        public async Task<RestaurantTableDto> GetAsync(EntityDto<int> input)
        {
            var table = await _tableRepository.GetAsync(input.Id);
            return await MapTableDtoAsync(table);
        }

        public async Task<RestaurantTableDto> CreateAsync(CreateRestaurantTableDto input)
        {
            var exists = await _tableRepository.GetAll()
                .AnyAsync(t => t.BranchId == input.BranchId && t.TableNo.ToLower() == input.TableNo.Trim().ToLower());

            if (exists)
            {
                throw new UserFriendlyException($"Table '{input.TableNo}' already exists in this branch.");
            }

            var isCarBay = input.IsCarBay
                || RestaurantTable.LooksLikeCarBay(input.TableNo, input.Area, false);

            var table = ObjectMapper.Map<RestaurantTable>(input);
            table.Status = TableStatus.Available;
            table.TableNo = input.TableNo.Trim();
            table.IsCarBay = isCarBay;
            table.Area = string.IsNullOrWhiteSpace(input.Area)
                ? (isCarBay ? "Car Parking" : "Main Hall")
                : input.Area.Trim();
            if (isCarBay && table.Capacity <= 0)
            {
                table.Capacity = 4;
            }

            var id = await _tableRepository.InsertAndGetIdAsync(table);
            table.Id = id;

            return await MapTableDtoAsync(table);
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
            table.IsCarBay = input.IsCarBay
                || RestaurantTable.LooksLikeCarBay(table.TableNo, table.Area, false);

            await _tableRepository.UpdateAsync(table);

            return await MapTableDtoAsync(table);
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
            return await MapTableDtoAsync(table);
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

                    var destIsCar = RestaurantTable.LooksLikeCarBay(dest.TableNo, dest.Area, dest.IsCarBay);
                    if (destIsCar)
                    {
                        order.OrderType = OrderType.CarService;
                        order.CarName = dest.TableNo;
                        order.CarLocation = string.IsNullOrWhiteSpace(dest.Area) ? "Car Parking" : dest.Area;
                        if (!string.IsNullOrWhiteSpace(order.CarNumber))
                        {
                            order.TableNo = $"Car: {order.CarName} - {order.CarNumber}";
                        }
                    }
                    else
                    {
                        order.OrderType = OrderType.DineIn;
                        order.CarName = null;
                        order.CarNumber = null;
                        order.CarLocation = null;
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

            return await MapTableDtoAsync(table);
        }

        private async Task<RestaurantTableDto> MapTableDtoAsync(RestaurantTable table)
        {
            var dto = ObjectMapper.Map<RestaurantTableDto>(table);
            dto.IsCarBay = RestaurantTable.LooksLikeCarBay(table.TableNo, table.Area, table.IsCarBay);
            if (table.CurrentOrderId.HasValue)
            {
                var order = await _orderRepository.GetAll()
                    .AsNoTracking()
                    .Where(o => o.Id == table.CurrentOrderId.Value)
                    .Select(o => new { o.CarNumber })
                    .FirstOrDefaultAsync();
                if (!string.IsNullOrWhiteSpace(order?.CarNumber))
                {
                    dto.CurrentVehicleNumber = order.CarNumber.Trim();
                }
            }

            return dto;
        }
    }
}
