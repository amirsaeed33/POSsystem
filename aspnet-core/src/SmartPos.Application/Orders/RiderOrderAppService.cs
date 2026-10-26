using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Events.Bus;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Branches;
using SmartPos.Deals;
using SmartPos.Orders;
using SmartPos.Orders.Dto;
using SmartPos.Products;
using SmartPos.Staffs;

namespace SmartPos.Orders
{
    [AbpAuthorize(PermissionNames.Pages_Rider)]
    public class RiderOrderAppService : SmartPosAppServiceBase, IRiderOrderAppService
    {
        public IEventBus EventBus { get; set; } = NullEventBus.Instance;

        private readonly IRepository<CustomerOrder> _orderRepository;
        private readonly IRepository<Staff> _staffRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<Deal> _dealRepository;
        private readonly IRepository<DealDetail> _dealDetailRepository;

        public RiderOrderAppService(
            IRepository<CustomerOrder> orderRepository,
            IRepository<Staff> staffRepository,
            IRepository<Branch> branchRepository,
            IRepository<Deal> dealRepository,
            IRepository<DealDetail> dealDetailRepository)
        {
            _orderRepository = orderRepository;
            _staffRepository = staffRepository;
            _branchRepository = branchRepository;
            _dealRepository = dealRepository;
            _dealDetailRepository = dealDetailRepository;
        }

        public async Task<RiderDashboardStatsDto> GetDashboardStatsAsync()
        {
            var staff = await GetCurrentRiderStaffAsync();

            var todayLocal = DateTime.Today;
            var todayUtc = DateTime.UtcNow.Date;

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var riderOrders = await _orderRepository.GetAll()
                    .Where(o => o.RiderId == staff.Id && o.BranchId == staff.BranchId)
                    .Select(o => new
                    {
                        o.Id,
                        o.Status,
                        o.TotalAmount,
                        o.OrderDate,
                        o.RiderAssignedTime,
                        o.LastModificationTime
                    })
                    .ToListAsync();

                var activeOrders = riderOrders
                    .Where(o => o.Status != CustomerOrderStatus.Delivered && o.Status != CustomerOrderStatus.Rejected)
                    .ToList();

                var deliveredOrdersToday = riderOrders
                    .Where(o => o.Status == CustomerOrderStatus.Delivered &&
                                ((o.LastModificationTime.HasValue && (o.LastModificationTime.Value.Date == todayLocal || o.LastModificationTime.Value.Date == todayUtc)) ||
                                 (o.OrderDate.Date == todayLocal || o.OrderDate.Date == todayUtc)))
                    .ToList();

                var totalAssignedToday = riderOrders
                    .Count(o => (o.RiderAssignedTime.HasValue && (o.RiderAssignedTime.Value.Date == todayLocal || o.RiderAssignedTime.Value.Date == todayUtc)) ||
                                (o.OrderDate.Date == todayLocal || o.OrderDate.Date == todayUtc));

                var branch = await _branchRepository.FirstOrDefaultAsync(staff.BranchId.Value);

                return new RiderDashboardStatsDto
                {
                    TodayDeliveredCount = deliveredOrdersToday.Count,
                    ActiveAssignedCount = activeOrders.Count,
                    TodayDeliveredAmount = deliveredOrdersToday.Sum(o => o.TotalAmount),
                    ActivePendingAmount = activeOrders.Sum(o => o.TotalAmount),
                    TotalAssignedToday = totalAssignedToday,
                    RiderStaffId = staff.Id,
                    RiderName = staff.Name,
                    RiderPhone = staff.Phone,
                    BranchName = branch?.Name
                };
            }
        }

        public async Task<List<RiderOrderDto>> GetAssignedOrdersAsync()
        {
            var staff = await GetCurrentRiderStaffAsync();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var query = _orderRepository.GetAll()
                    .Include(o => o.Customer)
                    .Include(o => o.Sale)
                    .Include(o => o.Lines)
                        .ThenInclude(l => l.Product)
                    .Where(o => o.RiderId == staff.Id &&
                                o.BranchId == staff.BranchId &&
                                o.Status != CustomerOrderStatus.Delivered &&
                                o.Status != CustomerOrderStatus.Rejected)
                    .OrderByDescending(o => o.RiderAssignedTime ?? o.OrderDate);

                var orders = await query.ToListAsync();
                return await MapToRiderOrderDtosAsync(orders);
            }
        }

        public async Task<PagedResultDto<RiderOrderDto>> GetOrderHistoryAsync(RiderOrderHistoryInput input)
        {
            var staff = await GetCurrentRiderStaffAsync();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var query = _orderRepository.GetAll()
                    .Include(o => o.Customer)
                    .Include(o => o.Sale)
                    .Include(o => o.Lines)
                        .ThenInclude(l => l.Product)
                    .Where(o => o.RiderId == staff.Id && o.BranchId == staff.BranchId);

                if (input.Status.HasValue)
                {
                    query = query.Where(o => o.Status == input.Status.Value);
                }
                else
                {
                    // By default in history, show delivered orders
                    query = query.Where(o => o.Status == CustomerOrderStatus.Delivered);
                }

                if (input.FromDate.HasValue)
                {
                    var from = input.FromDate.Value.Date;
                    query = query.Where(o => o.OrderDate >= from);
                }

                if (input.ToDate.HasValue)
                {
                    var to = input.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(o => o.OrderDate <= to);
                }

                if (!string.IsNullOrWhiteSpace(input.Keyword))
                {
                    var kw = input.Keyword.Trim();
                    query = query.Where(o =>
                        (o.OrderNo != null && o.OrderNo.Contains(kw)) ||
                        (o.Customer != null && o.Customer.Name.Contains(kw)) ||
                        (o.Customer != null && o.Customer.Phone.Contains(kw)) ||
                        (o.Customer != null && o.Customer.Address.Contains(kw)) ||
                        (o.Notes != null && o.Notes.Contains(kw)));
                }

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(o => o.LastModificationTime ?? o.OrderDate)
                    .PageBy(input)
                    .ToListAsync();

                var dtos = await MapToRiderOrderDtosAsync(items);

                return new PagedResultDto<RiderOrderDto>(totalCount, dtos);
            }
        }

        public async Task<RiderOrderDto> AcceptOrderAsync(EntityDto<int> input)
        {
            var staff = await GetCurrentRiderStaffAsync();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var order = await _orderRepository.GetAll()
                    .Include(o => o.Customer)
                    .Include(o => o.Sale)
                    .Include(o => o.Lines)
                        .ThenInclude(l => l.Product)
                    .FirstOrDefaultAsync(o => o.Id == input.Id);

                if (order == null)
                {
                    throw new UserFriendlyException("Order not found.");
                }

                if (order.RiderId != staff.Id)
                {
                    throw new UserFriendlyException("This order is not assigned to you.");
                }

                if (order.Status == CustomerOrderStatus.Delivered)
                {
                    throw new UserFriendlyException("Order is already delivered.");
                }

                if (order.Status == CustomerOrderStatus.Rejected)
                {
                    throw new UserFriendlyException("Order has been rejected and cannot be accepted.");
                }

                order.Status = CustomerOrderStatus.InProgress;
                await CurrentUnitOfWork.SaveChangesAsync();

                await EventBus.TriggerAsync(new CustomerOrderStatusChangedEventData(
                    order.Id,
                    order.OrderNo,
                    order.BranchId,
                    CustomerOrderStatus.InProgress));

                var dtos = await MapToRiderOrderDtosAsync(new List<CustomerOrder> { order });
                return dtos.FirstOrDefault();
            }
        }

        public async Task<RiderOrderDto> RejectOrderAsync(RejectRiderOrderDto input)
        {
            var staff = await GetCurrentRiderStaffAsync();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var order = await _orderRepository.GetAll()
                    .Include(o => o.Customer)
                    .Include(o => o.Sale)
                    .Include(o => o.Lines)
                        .ThenInclude(l => l.Product)
                    .FirstOrDefaultAsync(o => o.Id == input.OrderId);

                if (order == null)
                {
                    throw new UserFriendlyException("Order not found.");
                }

                if (order.RiderId != staff.Id)
                {
                    throw new UserFriendlyException("This order is not assigned to you.");
                }

                if (order.Status == CustomerOrderStatus.Delivered)
                {
                    throw new UserFriendlyException("Order is already marked as delivered.");
                }

                // Clear assignment and return status back to Approved for reassignment
                order.RiderId = null;
                order.RiderName = null;
                order.RiderPhone = null;
                order.RiderAssignedTime = null;
                order.Status = CustomerOrderStatus.Approved;

                var reason = !string.IsNullOrWhiteSpace(input.Reason) ? input.Reason.Trim() : "Declined by rider";
                var rejectNote = $"[Rider {staff.Name} declined assignment: {reason}]";
                order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                    ? rejectNote
                    : $"{order.Notes} {rejectNote}";

                await CurrentUnitOfWork.SaveChangesAsync();

                await EventBus.TriggerAsync(new CustomerOrderStatusChangedEventData(
                    order.Id,
                    order.OrderNo,
                    order.BranchId,
                    CustomerOrderStatus.Approved));

                var dtos = await MapToRiderOrderDtosAsync(new List<CustomerOrder> { order });
                return dtos.FirstOrDefault();
            }
        }

        public async Task<RiderOrderDto> CompleteDeliveryAsync(EntityDto<int> input)
        {
            var staff = await GetCurrentRiderStaffAsync();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var order = await _orderRepository.GetAll()
                    .Include(o => o.Customer)
                    .Include(o => o.Sale)
                    .Include(o => o.Lines)
                        .ThenInclude(l => l.Product)
                    .FirstOrDefaultAsync(o => o.Id == input.Id);

                if (order == null)
                {
                    throw new UserFriendlyException("Order not found.");
                }

                if (order.RiderId != staff.Id)
                {
                    throw new UserFriendlyException("This order is not assigned to you.");
                }

                if (order.Status == CustomerOrderStatus.Rejected)
                {
                    throw new UserFriendlyException("Rejected orders cannot be marked as delivered.");
                }

                order.Status = CustomerOrderStatus.Delivered;
                await CurrentUnitOfWork.SaveChangesAsync();

                await EventBus.TriggerAsync(new CustomerOrderStatusChangedEventData(
                    order.Id,
                    order.OrderNo,
                    order.BranchId,
                    CustomerOrderStatus.Delivered));

                var dtos = await MapToRiderOrderDtosAsync(new List<CustomerOrder> { order });
                return dtos.FirstOrDefault();
            }
        }

        private async Task<Staff> GetCurrentRiderStaffAsync()
        {
            if (!AbpSession.UserId.HasValue)
            {
                throw new AbpAuthorizationException("User is not logged in.");
            }

            var userId = AbpSession.UserId.Value;

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var staff = await _staffRepository.FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);
                if (staff == null)
                {
                    throw new UserFriendlyException("Current user account is not linked to any active staff member.");
                }

                if (!staff.IsActive)
                {
                    throw new UserFriendlyException("Your staff account is currently inactive.");
                }

                if (!staff.BranchId.HasValue)
                {
                    throw new UserFriendlyException("Staff is not assigned to a branch location.");
                }

                var branch = await _branchRepository.FirstOrDefaultAsync(staff.BranchId.Value);
                if (branch == null || branch.BusinessType != BusinessType.Restaurant)
                {
                    throw new UserFriendlyException("Delivery Rider Portal is only available for Restaurant branches.");
                }

                return staff;
            }
        }

        private async Task<List<RiderOrderDto>> MapToRiderOrderDtosAsync(List<CustomerOrder> orders)
        {
            if (orders == null || orders.Count == 0)
            {
                return new List<RiderOrderDto>();
            }

            // Pre-fetch any deals present in the order lines to display bundle breakdown
            var dealIds = orders
                .SelectMany(o => o.Lines ?? Enumerable.Empty<CustomerOrderLine>())
                .Where(l => l.Product != null && (l.Product.IsDeal || l.Product.DealId.HasValue))
                .Select(l => l.Product.DealId)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            Dictionary<int, List<string>> dealBreakdowns = new Dictionary<int, List<string>>();
            if (dealIds.Count > 0)
            {
                var dealDetails = await _dealDetailRepository.GetAll()
                    .Include(d => d.Product)
                    .Where(d => dealIds.Contains(d.DealId))
                    .ToListAsync();

                dealBreakdowns = dealDetails
                    .GroupBy(d => d.DealId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(d => $"{d.Quantity}x {d.Product?.Name ?? "Item"}").ToList());
            }

            var nowUtc = DateTime.UtcNow;

            return orders.Select(o =>
            {
                var statusText = o.Status switch
                {
                    CustomerOrderStatus.Pending => "Pending",
                    CustomerOrderStatus.Approved => "Assigned (Waiting Acceptance)",
                    CustomerOrderStatus.InProgress => "Accepted & In Transit",
                    CustomerOrderStatus.Delivered => "Delivered",
                    CustomerOrderStatus.Rejected => "Rejected",
                    _ => o.Status.ToString()
                };

                string assignedTimeText = "Recently";
                if (o.RiderAssignedTime.HasValue)
                {
                    var diff = nowUtc - o.RiderAssignedTime.Value.ToUniversalTime();
                    if (diff.TotalMinutes < 1) assignedTimeText = "Just now";
                    else if (diff.TotalMinutes < 60) assignedTimeText = $"{(int)diff.TotalMinutes}m ago";
                    else if (diff.TotalHours < 24) assignedTimeText = $"{(int)diff.TotalHours}h ago";
                    else assignedTimeText = o.RiderAssignedTime.Value.ToString("MMM dd, yyyy");
                }

                var items = (o.Lines ?? Enumerable.Empty<CustomerOrderLine>()).Select(l =>
                {
                    var isDeal = l.Product != null && (l.Product.IsDeal || l.Product.DealId.HasValue);
                    List<string> breakdowns = new List<string>();
                    if (isDeal && l.Product?.DealId.HasValue == true && dealBreakdowns.TryGetValue(l.Product.DealId.Value, out var list))
                    {
                        breakdowns = list;
                    }

                    return new RiderOrderItemDto
                    {
                        ProductId = l.ProductId,
                        ProductName = l.Product?.Name ?? $"Item #{l.ProductId}",
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice,
                        LineTotal = l.LineTotal,
                        IsDeal = isDeal,
                        DealItemDescriptions = breakdowns
                    };
                }).ToList();

                var paymentMethod = "Cash on Delivery";
                if (o.Sale != null && !string.IsNullOrWhiteSpace(o.Sale.InvoiceNo))
                {
                    paymentMethod = "POS Invoice Linked";
                }

                return new RiderOrderDto
                {
                    Id = o.Id,
                    OrderNo = !string.IsNullOrWhiteSpace(o.OrderNo) ? o.OrderNo : $"#{o.Id}",
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    StatusText = statusText,
                    RiderAssignedTime = o.RiderAssignedTime,
                    RiderAssignedTimeText = assignedTimeText,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer?.Name ?? "Guest Customer",
                    CustomerPhone = o.Customer?.Phone,
                    CustomerAddress = o.Customer?.Address,
                    DeliveryAreaName = o.DeliveryAreaName,
                    Latitude = o.Latitude,
                    Longitude = o.Longitude,
                    DistanceKm = o.DistanceKm,
                    SubTotal = o.SubTotal,
                    DeliveryFee = o.DeliveryFee,
                    TotalAmount = o.TotalAmount,
                    Notes = o.Notes,
                    PaymentMethod = paymentMethod,
                    IsAcceptedByRider = o.Status == CustomerOrderStatus.InProgress,
                    Items = items
                };
            }).ToList();
        }
    }
}
