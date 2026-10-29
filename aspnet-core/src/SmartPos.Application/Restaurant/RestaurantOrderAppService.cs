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
using SmartPos.Authorization.Roles;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Restaurant.Dto;
using SmartPos.Sales;
using SmartPos.Sales.Dto;

namespace SmartPos.Restaurant
{
    [AbpAuthorize]
    public class RestaurantOrderAppService : ApplicationService, IRestaurantOrderAppService
    {
        private readonly IRepository<RestaurantOrder> _orderRepository;
        private readonly IRepository<RestaurantOrderLine> _lineRepository;
        private readonly IRepository<RestaurantTable> _tableRepository;
        private readonly ISaleAppService _saleAppService;
        private readonly IRepository<RestaurantPrintLog> _printLogRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly UserManager _userManager;

        public RestaurantOrderAppService(
            IRepository<RestaurantOrder> orderRepository,
            IRepository<RestaurantOrderLine> lineRepository,
            IRepository<RestaurantTable> tableRepository,
            ISaleAppService saleAppService,
            IRepository<RestaurantPrintLog> printLogRepository,
            IRepository<Branch> branchRepository,
            IBranchAccessChecker branchAccessChecker,
            UserManager userManager)
        {
            _orderRepository = orderRepository;
            _lineRepository = lineRepository;
            _tableRepository = tableRepository;
            _saleAppService = saleAppService;
            _printLogRepository = printLogRepository;
            _branchRepository = branchRepository;
            _branchAccessChecker = branchAccessChecker;
            _userManager = userManager;
        }

        private async Task<(long UserId, string Name, string Role)> GetCurrentPrinterIdentityAsync()
        {
            if (!AbpSession.UserId.HasValue)
            {
                return (0, "Staff", "Staff");
            }

            var user = await _userManager.FindByIdAsync(AbpSession.UserId.Value.ToString());
            if (user == null)
            {
                return (AbpSession.UserId.Value, "Staff", "Staff");
            }

            var fullName = $"{user.Name} {user.Surname}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = user.UserName;
            }

            var roles = await _userManager.GetRolesAsync(user);
            string primaryRole = "Staff";
            if (roles.Contains(StaticRoleNames.Tenants.Waiter))
            {
                primaryRole = "Waiter";
            }
            else if (roles.Contains(StaticRoleNames.Tenants.Admin))
            {
                primaryRole = "Admin";
            }
            else if (roles.Any())
            {
                primaryRole = roles.First();
            }

            return (user.Id, fullName, primaryRole);
        }

        private async Task<bool> IsCurrentUserWaiterAsync()
        {
            if (!AbpSession.UserId.HasValue) return false;
            var currentUser = await _userManager.FindByIdAsync(AbpSession.UserId.Value.ToString());
            if (currentUser == null) return false;
            var roles = await _userManager.GetRolesAsync(currentUser);
            return roles.Contains(StaticRoleNames.Tenants.Waiter);
        }

        public async Task<List<RestaurantOrderDto>> GetActiveOrdersAsync(int branchId)
        {
            await _branchAccessChecker.EnsureCanAccessBranchAsync(branchId);
            var orders = await _orderRepository.GetAll()
                .Include(o => o.Lines)
                .Include(o => o.Customer)
                .Where(o => o.BranchId == branchId &&
                            (o.Status == RestaurantOrderStatus.Open || o.Status == RestaurantOrderStatus.Billed))
                .OrderByDescending(o => o.CreationTime)
                .ToListAsync();

            return ObjectMapper.Map<List<RestaurantOrderDto>>(orders);
        }

        public async Task<RestaurantOrderDto> GetAsync(EntityDto<int> input)
        {
            var order = await _orderRepository.GetAll()
                .Include(o => o.Lines)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == input.Id);

            if (order == null)
            {
                throw new UserFriendlyException("Order not found.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);
            return ObjectMapper.Map<RestaurantOrderDto>(order);
        }

        public async Task<RestaurantOrderDto> GetByTableAsync(int tableId)
        {
            var table = await _tableRepository.FirstOrDefaultAsync(tableId);
            if (table == null)
            {
                throw new UserFriendlyException("Table not found.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(table.BranchId);

            var order = await _orderRepository.GetAll()
                .Include(o => o.Lines)
                .Include(o => o.Customer)
                .Where(o => o.TableId == tableId &&
                            (o.Status == RestaurantOrderStatus.Open || o.Status == RestaurantOrderStatus.Billed))
                .OrderByDescending(o => o.CreationTime)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return null;
            }

            return ObjectMapper.Map<RestaurantOrderDto>(order);
        }

        public async Task<RestaurantOrderDto> SaveOrderAsync(CreateOrUpdateRestaurantOrderDto input)
        {
            if (input == null || input.Lines == null || !input.Lines.Any())
            {
                throw new UserFriendlyException("Order must have at least one line item.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(input.BranchId);

            RestaurantOrder order;
            bool isNew = false;

            if (input.Id.HasValue && input.Id.Value > 0)
            {
                order = await _orderRepository.GetAll()
                    .Include(o => o.Lines)
                    .FirstOrDefaultAsync(o => o.Id == input.Id.Value);

                if (order == null)
                {
                    throw new UserFriendlyException("Order not found.");
                }
            }
            else if (input.TableId.HasValue && input.TableId.Value > 0)
            {
                order = await _orderRepository.GetAll()
                    .Include(o => o.Lines)
                    .Where(o => o.TableId == input.TableId.Value &&
                                (o.Status == RestaurantOrderStatus.Open || o.Status == RestaurantOrderStatus.Billed))
                    .OrderByDescending(o => o.CreationTime)
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    order = new RestaurantOrder
                    {
                        BranchId = input.BranchId,
                        OrderNo = GenerateOrderNumber(),
                        TableId = input.TableId,
                        TableNo = input.TableNo,
                        Status = RestaurantOrderStatus.Open,
                        OrderType = input.OrderType
                    };
                    isNew = true;
                }
            }
            else
            {
                var isCar = input.OrderType == OrderType.CarService || !string.IsNullOrWhiteSpace(input.CarNumber);
                order = new RestaurantOrder
                {
                    BranchId = input.BranchId,
                    OrderNo = GenerateOrderNumber(),
                    TableId = null,
                    TableNo = isCar ? $"Car: {input.CarName} - {input.CarNumber}" : "Takeaway",
                    CarName = input.CarName,
                    CarNumber = input.CarNumber,
                    CarLocation = input.CarLocation,
                    Status = RestaurantOrderStatus.Open,
                    OrderType = isCar ? OrderType.CarService : input.OrderType
                };
                isNew = true;
            }

            var isWaiter = await IsCurrentUserWaiterAsync();
            order.CustomerId = input.CustomerId;
            if (isWaiter)
            {
                var printer = await GetCurrentPrinterIdentityAsync();
                order.WaiterName = printer.Name;
            }
            else
            {
                order.WaiterName = input.WaiterName;
                if (string.IsNullOrWhiteSpace(order.WaiterName) && order.TableId.HasValue)
                {
                    var tbl = await _tableRepository.FirstOrDefaultAsync(order.TableId.Value);
                    if (!string.IsNullOrWhiteSpace(tbl?.WaiterName))
                    {
                        order.WaiterName = tbl.WaiterName;
                    }
                }
            }
            order.Notes = input.Notes;
            order.OrderType = input.OrderType;
            if (!string.IsNullOrWhiteSpace(input.CarName)) order.CarName = input.CarName;
            if (!string.IsNullOrWhiteSpace(input.CarNumber)) order.CarNumber = input.CarNumber;
            if (!string.IsNullOrWhiteSpace(input.CarLocation)) order.CarLocation = input.CarLocation;
            if (!string.IsNullOrWhiteSpace(input.TableNo))
            {
                order.TableNo = input.TableNo;
            }
            else if (order.OrderType == OrderType.CarService && !string.IsNullOrWhiteSpace(order.CarNumber))
            {
                order.TableNo = $"Car: {order.CarName} - {order.CarNumber}";
            }

            decimal subTotal = 0;

            // Handle removal of lines
            var inputLineIds = input.Lines.Where(l => l.Id.HasValue).Select(l => l.Id.Value).ToList();
            if (isWaiter || order.OrderType == OrderType.Takeaway)
            {
                // For Waiters or Takeaway KOT: items already sent to kitchen (KOT) or printed cannot be removed by line deletion.
                // An active KOT order must be paid/settled or explicitly cancelled via CancelOrderAsync.
                var unprintedLinesToRemove = order.Lines.Where(l => !l.IsSentToKitchen && l.PrintedQuantity <= 0 && !inputLineIds.Contains(l.Id)).ToList();
                foreach (var rem in unprintedLinesToRemove)
                {
                    order.Lines.Remove(rem);
                }
            }
            else
            {
                // Admin can remove any lines
                var linesToRemove = order.Lines.Where(l => !inputLineIds.Contains(l.Id)).ToList();
                foreach (var rem in linesToRemove)
                {
                    order.Lines.Remove(rem);
                }
            }

            foreach (var lDto in input.Lines)
            {
                var lineTotal = Math.Round(lDto.Quantity * lDto.UnitPrice, 2);
                subTotal += lineTotal;

                if (lDto.Id.HasValue && lDto.Id.Value > 0)
                {
                    var existingLine = order.Lines.FirstOrDefault(l => l.Id == lDto.Id.Value);
                    if (existingLine != null)
                    {
                        if (isWaiter && (existingLine.IsSentToKitchen || existingLine.PrintedQuantity > 0))
                        {
                            if (lDto.Quantity < existingLine.PrintedQuantity || lDto.Quantity < existingLine.Quantity)
                            {
                                throw new UserFriendlyException($"Waiters cannot deduct or decrease quantity for '{existingLine.ProductName}'.");
                            }
                        }

                        existingLine.Quantity = lDto.Quantity;
                        existingLine.UnitPrice = lDto.UnitPrice;
                        existingLine.LineTotal = lineTotal;
                        existingLine.KitchenNotes = lDto.KitchenNotes;
                        existingLine.SelectedModifiers = lDto.SelectedModifiers;
                        existingLine.IsSentToKitchen = existingLine.IsSentToKitchen || lDto.IsSentToKitchen || existingLine.PrintedQuantity > 0;
                        if (lDto.PrintedQuantity > existingLine.PrintedQuantity)
                        {
                            existingLine.PrintedQuantity = lDto.PrintedQuantity;
                        }
                        else if (!isWaiter && existingLine.PrintedQuantity > existingLine.Quantity)
                        {
                            existingLine.PrintedQuantity = existingLine.Quantity;
                        }
                        continue;
                    }
                }

                order.Lines.Add(new RestaurantOrderLine
                {
                    ProductId = lDto.ProductId,
                    ProductName = lDto.ProductName,
                    Quantity = lDto.Quantity,
                    UnitPrice = lDto.UnitPrice,
                    LineTotal = lineTotal,
                    KitchenNotes = lDto.KitchenNotes,
                    SelectedModifiers = lDto.SelectedModifiers,
                    IsSentToKitchen = lDto.IsSentToKitchen,
                    PrintedQuantity = lDto.PrintedQuantity
                });
            }

            order.SubTotal = subTotal;
            order.DiscountPercent = input.DiscountPercent;
            decimal computedDiscount = input.DiscountAmount;
            if (input.DiscountPercent > 0 && computedDiscount <= 0)
            {
                computedDiscount = Math.Round((subTotal * input.DiscountPercent) / 100m, 2);
            }
            order.DiscountAmount = Math.Min(subTotal, Math.Max(0, computedDiscount));

            decimal taxable = subTotal - order.DiscountAmount;
            order.TaxPercent = input.TaxPercent;
            order.TaxAmount = input.TaxPercent > 0 ? Math.Round((taxable * input.TaxPercent) / 100m, 2) : 0;

            order.ServiceChargePercent = input.ServiceChargePercent;
            order.ServiceChargeAmount = (input.ServiceChargePercent > 0 && (input.OrderType == OrderType.DineIn || input.OrderType == OrderType.CarService))
                ? Math.Round((taxable * input.ServiceChargePercent) / 100m, 2)
                : 0;

            order.GrandTotal = Math.Round(taxable + order.TaxAmount + order.ServiceChargeAmount, 2);

            if (isNew)
            {
                var id = await _orderRepository.InsertAndGetIdAsync(order);
                order.Id = id;
            }
            else
            {
                await _orderRepository.UpdateAsync(order);
            }

            // Sync table status
            if (order.TableId.HasValue)
            {
                var table = await _tableRepository.GetAsync(order.TableId.Value);
                table.Status = TableStatus.Occupied;
                table.CurrentOrderId = order.Id;
                if (!string.IsNullOrWhiteSpace(order.WaiterName))
                {
                    table.WaiterName = order.WaiterName;
                }
                await _tableRepository.UpdateAsync(table);
            }

            return ObjectMapper.Map<RestaurantOrderDto>(order);
        }

        public async Task<RestaurantOrderDto> ReserveCarServiceAsync(ReserveCarServiceDto input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.CarName))
                throw new UserFriendlyException("Car Name is required.");
            if (string.IsNullOrWhiteSpace(input.CarNumber))
                throw new UserFriendlyException("Car Number is required.");
            if (string.IsNullOrWhiteSpace(input.CarLocation))
                throw new UserFriendlyException("Car Location is required.");

            await _branchAccessChecker.EnsureCanAccessBranchAsync(input.BranchId);

            var isWaiter = await IsCurrentUserWaiterAsync();
            string waiter = input.WaiterName;
            if (isWaiter)
            {
                var printer = await GetCurrentPrinterIdentityAsync();
                waiter = printer.Name;
            }

            var order = new RestaurantOrder
            {
                TenantId = AbpSession.TenantId,
                BranchId = input.BranchId,
                OrderNo = GenerateOrderNumber(),
                TableId = null,
                TableNo = $"Car: {input.CarName.Trim()} - {input.CarNumber.Trim()}",
                OrderType = OrderType.CarService,
                Status = RestaurantOrderStatus.Open,
                CarName = input.CarName.Trim(),
                CarNumber = input.CarNumber.Trim(),
                CarLocation = input.CarLocation.Trim(),
                WaiterName = string.IsNullOrWhiteSpace(waiter) ? "Staff" : waiter.Trim(),
                Notes = input.Notes,
                Lines = new List<RestaurantOrderLine>()
            };

            var id = await _orderRepository.InsertAndGetIdAsync(order);
            await CurrentUnitOfWork.SaveChangesAsync();

            var saved = await _orderRepository.GetAll()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == id);

            return ObjectMapper.Map<RestaurantOrderDto>(saved);
        }

        public async Task<List<RestaurantOrderDto>> GetActiveCarOrdersAsync(int branchId)
        {
            await _branchAccessChecker.EnsureCanAccessBranchAsync(branchId);
            var orders = await _orderRepository.GetAll()
                .Include(o => o.Lines)
                .Include(o => o.Customer)
                .Where(o => o.BranchId == branchId &&
                            (o.Status == RestaurantOrderStatus.Open || o.Status == RestaurantOrderStatus.Billed) &&
                            (o.OrderType == OrderType.CarService || !string.IsNullOrEmpty(o.CarNumber)))
                .OrderByDescending(o => o.CreationTime)
                .ToListAsync();

            return ObjectMapper.Map<List<RestaurantOrderDto>>(orders);
        }

        public async Task<SendToKitchenResultDto> SendToKitchenAsync(int orderId)
        {
            var order = await _orderRepository.GetAll()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                throw new UserFriendlyException("Order not found.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

            var printer = await GetCurrentPrinterIdentityAsync();

            // Smart KOT: Calculate delta items (Quantity - PrintedQuantity)
            var itemsToPrint = new List<KitchenSlipItemDto>();
            foreach (var line in order.Lines)
            {
                var additionalQty = line.Quantity - line.PrintedQuantity;
                if (additionalQty > 0)
                {
                    line.IsSentToKitchen = true;
                    itemsToPrint.Add(new KitchenSlipItemDto
                    {
                        LineId = line.Id,
                        ProductName = line.ProductName,
                        Quantity = additionalQty,
                        PreviousQuantity = line.PrintedQuantity,
                        TotalQuantity = line.Quantity,
                        KitchenNotes = line.KitchenNotes,
                        SelectedModifiers = line.SelectedModifiers
                    });
                }
            }

            if (itemsToPrint.Any())
            {
                await _orderRepository.UpdateAsync(order);
            }

            var isCarOrder = order.OrderType == OrderType.CarService || !string.IsNullOrWhiteSpace(order.CarNumber);
            var displayTableNo = isCarOrder
                ? $"Car: {order.CarName} - {order.CarNumber} ({order.CarLocation})"
                : (order.TableNo ?? "Takeaway");

            // Never reprint previously sent items, whether admin or waiter
            if (!itemsToPrint.Any())
            {
                return new SendToKitchenResultDto
                {
                    OrderId = order.Id,
                    OrderNo = order.OrderNo,
                    TableNo = displayTableNo,
                    CarName = order.CarName,
                    CarNumber = order.CarNumber,
                    CarLocation = order.CarLocation,
                    WaiterName = order.WaiterName ?? "Staff",
                    PrintTime = DateTime.UtcNow,
                    PrintedBy = $"{printer.Name} ({printer.Role})",
                    PrintedByUserId = printer.UserId,
                    PrintedByName = printer.Name,
                    PrintedByRole = printer.Role,
                    HasNewItems = false,
                    Message = "All items for this order have already been sent to kitchen.",
                    NewItems = new List<KitchenSlipItemDto>()
                };
            }

            // Do not prematurely mark items as printed here.
            // Items are reliably marked as printed when printing is confirmed via ConfirmKotPrintAsync.
            return new SendToKitchenResultDto
            {
                OrderId = order.Id,
                OrderNo = order.OrderNo,
                TableNo = displayTableNo,
                CarName = order.CarName,
                CarNumber = order.CarNumber,
                CarLocation = order.CarLocation,
                WaiterName = order.WaiterName ?? "Staff",
                PrintTime = DateTime.UtcNow,
                PrintedBy = $"{printer.Name} ({printer.Role})",
                PrintedByUserId = printer.UserId,
                PrintedByName = printer.Name,
                PrintedByRole = printer.Role,
                HasNewItems = true,
                Message = "New items ready to send to kitchen.",
                NewItems = itemsToPrint
            };
        }

        public async Task<SendToKitchenResultDto> ConfirmKotPrintAsync(ConfirmKotPrintDto input)
        {
            var order = await _orderRepository.GetAll()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == input.OrderId);

            if (order == null)
            {
                throw new UserFriendlyException("Order not found.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

            var now = DateTime.UtcNow;
            var targetLines = input.LineIds != null && input.LineIds.Any()
                ? order.Lines.Where(l => input.LineIds.Contains(l.Id)).ToList()
                : order.Lines.Where(l => l.Quantity > l.PrintedQuantity).ToList();

            foreach (var line in targetLines)
            {
                line.PrintedQuantity = line.Quantity;
                line.IsSentToKitchen = true;
                line.SentToKitchenTime = now;
            }

            var printer = await GetCurrentPrinterIdentityAsync();
            order.LastPrintedBy = $"{printer.Name} ({printer.Role})";
            order.LastPrintedTime = now;
            await _orderRepository.UpdateAsync(order);

            await _printLogRepository.InsertAsync(new RestaurantPrintLog
            {
                TenantId = order.TenantId,
                BranchId = order.BranchId,
                OrderId = order.Id,
                OrderNo = order.OrderNo,
                ReceiptType = "KOT",
                PrintedByUserId = printer.UserId,
                PrintedByName = printer.Name,
                PrintedByRole = printer.Role,
                PrintTime = now
            });

            await CurrentUnitOfWork.SaveChangesAsync();

            var isCarOrder = order.OrderType == OrderType.CarService || !string.IsNullOrWhiteSpace(order.CarNumber);
            var displayTableNo = isCarOrder
                ? $"Car: {order.CarName} - {order.CarNumber} ({order.CarLocation})"
                : (order.TableNo ?? "Takeaway");

            return new SendToKitchenResultDto
            {
                OrderId = order.Id,
                OrderNo = order.OrderNo,
                TableNo = displayTableNo,
                CarName = order.CarName,
                CarNumber = order.CarNumber,
                CarLocation = order.CarLocation,
                WaiterName = order.WaiterName ?? "Staff",
                PrintTime = now,
                PrintedBy = $"{printer.Name} ({printer.Role})",
                PrintedByUserId = printer.UserId,
                PrintedByName = printer.Name,
                PrintedByRole = printer.Role,
                HasNewItems = true,
                NewItems = targetLines.Select(l => new KitchenSlipItemDto
                {
                    LineId = l.Id,
                    ProductName = l.ProductName,
                    Quantity = l.Quantity,
                    KitchenNotes = l.KitchenNotes,
                    SelectedModifiers = l.SelectedModifiers
                }).ToList()
            };
        }

        public async Task<RestaurantOrderDto> PrintBillAsync(int orderId)
        {
            var order = await _orderRepository.GetAll()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                throw new UserFriendlyException("Order not found.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

            var printer = await GetCurrentPrinterIdentityAsync();
            order.LastPrintedBy = $"{printer.Name} ({printer.Role})";
            order.LastPrintedTime = DateTime.UtcNow;
            order.Status = RestaurantOrderStatus.Billed;
            order.BilledTime = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order);

            if (order.TableId.HasValue)
            {
                var table = await _tableRepository.GetAsync(order.TableId.Value);
                table.Status = TableStatus.Billed;
                await _tableRepository.UpdateAsync(table);
            }

            await _printLogRepository.InsertAsync(new RestaurantPrintLog
            {
                TenantId = order.TenantId,
                BranchId = order.BranchId,
                OrderId = order.Id,
                OrderNo = order.OrderNo,
                ReceiptType = "GuestBill",
                PrintedByUserId = printer.UserId,
                PrintedByName = printer.Name,
                PrintedByRole = printer.Role,
                PrintTime = DateTime.UtcNow
            });

            return ObjectMapper.Map<RestaurantOrderDto>(order);
        }

        public async Task<int> SettleOrderAsync(SettleRestaurantOrderDto input)
        {
            var order = await _orderRepository.GetAll()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == input.OrderId);

            if (order == null)
            {
                throw new UserFriendlyException("Order not found.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

            // Waiters must NOT be allowed to settle payments, receive payments, or close unpaid bills through payment settlement
            if (AbpSession.UserId.HasValue)
            {
                var currentUser = await _userManager.FindByIdAsync(AbpSession.UserId.Value.ToString());
                if (currentUser != null)
                {
                    var roles = await _userManager.GetRolesAsync(currentUser);
                    if (roles.Contains(StaticRoleNames.Tenants.Waiter))
                    {
                        throw new UserFriendlyException("Waiters are not authorized to settle payments or close bills.");
                    }
                }
            }

            if (order.Status == RestaurantOrderStatus.Settled)
            {
                throw new UserFriendlyException("Order is already settled.");
            }

            string waiterTag = !string.IsNullOrWhiteSpace(order.WaiterName) ? $" [Waiter: {order.WaiterName}]" : "";
            string tableTag;
            if (order.OrderType == OrderType.CarService || !string.IsNullOrWhiteSpace(order.CarNumber))
            {
                tableTag = $"[Car Service - {order.CarName} ({order.CarNumber}) - {order.CarLocation}]{waiterTag}";
            }
            else if (order.OrderType == OrderType.DineIn)
            {
                tableTag = $"[Dine-In - Table {order.TableNo}]{waiterTag}";
            }
            else
            {
                tableTag = "[Takeaway]";
            }
            string finalNotes = string.IsNullOrWhiteSpace(input.Notes)
                ? tableTag
                : $"{tableTag} {input.Notes.Trim()}";

            var customerId = input.CustomerId > 0 
                ? input.CustomerId 
                : (order.CustomerId.HasValue && order.CustomerId.Value > 0 ? order.CustomerId.Value : 1);

            // Settle order via SaleAppService
            var saleDto = new CreateSaleDto
            {
                CustomerId = customerId,
                SaleDate = DateTime.UtcNow,
                Notes = finalNotes,
                DiscountAmount = order.DiscountAmount,
                DiscountPercent = order.DiscountPercent,
                TaxPercent = order.TaxPercent,
                PaymentType = input.PaymentType,
                CashAmount = input.CashAmount,
                CardAmount = input.CardAmount,
                Lines = order.Lines.Select(l => new CreateSaleLineDto
                {
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice
                }).ToList()
            };

            var createdSale = await _saleAppService.CreateAsync(saleDto);

            var printer = await GetCurrentPrinterIdentityAsync();
            order.Status = RestaurantOrderStatus.Settled;
            order.SaleId = createdSale.Id;
            order.SettledTime = DateTime.UtcNow;
            order.LastPrintedBy = $"{printer.Name} ({printer.Role})";
            order.LastPrintedTime = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order);

            await _printLogRepository.InsertAsync(new RestaurantPrintLog
            {
                TenantId = order.TenantId,
                BranchId = order.BranchId,
                OrderId = order.Id,
                OrderNo = order.OrderNo,
                ReceiptType = "SaleReceipt",
                PrintedByUserId = printer.UserId,
                PrintedByName = printer.Name,
                PrintedByRole = printer.Role,
                PrintTime = DateTime.UtcNow
            });

            // Free table back to Available
            if (order.TableId.HasValue)
            {
                var table = await _tableRepository.GetAsync(order.TableId.Value);
                table.Status = TableStatus.Available;
                table.CurrentOrderId = null;
                table.WaiterId = null;
                table.WaiterName = null;
                await _tableRepository.UpdateAsync(table);
            }

            return createdSale.Id;
        }

        public async Task<RestaurantPrintLogDto> RecordPrintLogAsync(RecordPrintLogDto input)
        {
            await _branchAccessChecker.EnsureCanAccessBranchAsync(input.BranchId);
            var branch = await _branchRepository.FirstOrDefaultAsync(input.BranchId);
            if (branch == null || branch.BusinessType != BusinessType.Restaurant)
            {
                throw new UserFriendlyException("Print tracking is only supported for restaurant branches.");
            }

            var printer = await GetCurrentPrinterIdentityAsync();
            var log = new RestaurantPrintLog
            {
                TenantId = AbpSession.TenantId,
                BranchId = input.BranchId,
                OrderId = input.OrderId,
                OrderNo = input.OrderNo,
                ReceiptType = input.ReceiptType,
                PrintedByUserId = printer.UserId,
                PrintedByName = printer.Name,
                PrintedByRole = printer.Role,
                PrintTime = DateTime.UtcNow
            };
            await _printLogRepository.InsertAsync(log);

            if (input.OrderId.HasValue && input.OrderId.Value > 0)
            {
                var order = await _orderRepository.FirstOrDefaultAsync(input.OrderId.Value);
                if (order != null)
                {
                    order.LastPrintedBy = $"{printer.Name} ({printer.Role})";
                    order.LastPrintedTime = DateTime.UtcNow;
                    await _orderRepository.UpdateAsync(order);
                }
            }

            return new RestaurantPrintLogDto
            {
                Id = log.Id,
                BranchId = log.BranchId,
                OrderId = log.OrderId,
                OrderNo = log.OrderNo,
                ReceiptType = log.ReceiptType,
                PrintedByUserId = log.PrintedByUserId,
                PrintedByName = log.PrintedByName,
                PrintedByRole = log.PrintedByRole,
                PrintTime = log.PrintTime
            };
        }

        public async Task<List<RestaurantPrintLogDto>> GetOrderPrintLogsAsync(int orderId)
        {
            var order = await _orderRepository.GetAsync(orderId);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

            var logs = await _printLogRepository.GetAll()
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.PrintTime)
                .ToListAsync();

            return logs.Select(l => new RestaurantPrintLogDto
            {
                Id = l.Id,
                BranchId = l.BranchId,
                OrderId = l.OrderId,
                OrderNo = l.OrderNo,
                ReceiptType = l.ReceiptType,
                PrintedByUserId = l.PrintedByUserId,
                PrintedByName = l.PrintedByName,
                PrintedByRole = l.PrintedByRole,
                PrintTime = l.PrintTime
            }).ToList();
        }

        public async Task<List<UnpaidTakeawayOrderDto>> GetUnpaidTakeawayOrdersAsync(int branchId)
        {
            var branch = await _branchRepository.FirstOrDefaultAsync(branchId);
            if (branch == null || branch.BusinessType != BusinessType.Restaurant)
            {
                return new List<UnpaidTakeawayOrderDto>();
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(branchId);

            var orders = await _orderRepository.GetAll()
                .Where(o => o.BranchId == branchId &&
                            o.OrderType == OrderType.Takeaway &&
                            (o.Status == RestaurantOrderStatus.Open || o.Status == RestaurantOrderStatus.Billed) &&
                            (o.Lines.Any(l => l.IsSentToKitchen || l.PrintedQuantity > 0) || o.LastPrintedTime != null))
                .OrderByDescending(o => o.CreationTime)
                .Select(o => new UnpaidTakeawayOrderDto
                {
                    Id = o.Id,
                    OrderNo = o.OrderNo,
                    CreationTime = o.CreationTime,
                    Status = o.Status.ToString(),
                    GrandTotal = o.GrandTotal,
                    WaiterName = o.WaiterName,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Walk-in Customer",
                    ItemCount = o.Lines.Count
                })
                .ToListAsync();

            return orders;
        }

        public async Task CancelOrderAsync(int orderId)
        {
            if (await IsCurrentUserWaiterAsync())
            {
                throw new UserFriendlyException("Waiters are not authorized to cancel orders.");
            }

            var order = await _orderRepository.GetAsync(orderId);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

            if (order.Status == RestaurantOrderStatus.Settled)
            {
                throw new UserFriendlyException("Paid and settled orders cannot be cancelled.");
            }

            order.Status = RestaurantOrderStatus.Cancelled;
            await _orderRepository.UpdateAsync(order);

            if (order.TableId.HasValue)
            {
                var table = await _tableRepository.GetAsync(order.TableId.Value);
                table.Status = TableStatus.Available;
                table.CurrentOrderId = null;
                await _tableRepository.UpdateAsync(table);
            }
        }

        public async Task<RestaurantOrderDto> SplitBillAsync(SplitBillDto input)
        {
            var source = await _orderRepository.GetAll()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == input.SourceOrderId);

            if (source == null)
            {
                throw new UserFriendlyException("Source order not found.");
            }

            if (input.SplitEqually)
            {
                // Simply return source order - the client prints equal sub-checks
                return ObjectMapper.Map<RestaurantOrderDto>(source);
            }

            if (input.MoveLineIds == null || !input.MoveLineIds.Any())
            {
                throw new UserFriendlyException("Select items to split.");
            }

            var movedLines = source.Lines.Where(l => input.MoveLineIds.Contains(l.Id)).ToList();
            if (!movedLines.Any())
            {
                throw new UserFriendlyException("No matching lines to split.");
            }

            // Create secondary order
            var subOrder = new RestaurantOrder
            {
                BranchId = source.BranchId,
                OrderNo = GenerateOrderNumber(),
                TableId = source.TableId,
                TableNo = $"{source.TableNo} (Split)",
                OrderType = source.OrderType,
                CustomerId = source.CustomerId,
                WaiterName = source.WaiterName,
                Status = RestaurantOrderStatus.Open,
                TaxPercent = source.TaxPercent,
                ServiceChargePercent = source.ServiceChargePercent,
                Lines = new List<RestaurantOrderLine>()
            };

            decimal movedSubTotal = 0;
            foreach (var line in movedLines)
            {
                source.Lines.Remove(line);
                subOrder.Lines.Add(new RestaurantOrderLine
                {
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    LineTotal = line.LineTotal,
                    KitchenNotes = line.KitchenNotes,
                    SelectedModifiers = line.SelectedModifiers,
                    IsSentToKitchen = true
                });
                movedSubTotal += line.LineTotal;
            }

            // Recalculate subOrder totals
            subOrder.SubTotal = movedSubTotal;
            subOrder.TaxAmount = (subOrder.TaxPercent > 0) ? Math.Round((movedSubTotal * subOrder.TaxPercent) / 100m, 2) : 0;
            subOrder.ServiceChargeAmount = (subOrder.ServiceChargePercent > 0 && subOrder.OrderType == OrderType.DineIn)
                ? Math.Round((movedSubTotal * subOrder.ServiceChargePercent) / 100m, 2)
                : 0;
            subOrder.GrandTotal = subOrder.SubTotal + subOrder.TaxAmount + subOrder.ServiceChargeAmount;

            // Recalculate source totals
            source.SubTotal = source.Lines.Sum(l => l.LineTotal);
            source.TaxAmount = (source.TaxPercent > 0) ? Math.Round((source.SubTotal * source.TaxPercent) / 100m, 2) : 0;
            source.ServiceChargeAmount = (source.ServiceChargePercent > 0 && source.OrderType == OrderType.DineIn)
                ? Math.Round((source.SubTotal * source.ServiceChargePercent) / 100m, 2)
                : 0;
            source.GrandTotal = source.SubTotal + source.TaxAmount + source.ServiceChargeAmount;

            await _orderRepository.InsertAsync(subOrder);
            await _orderRepository.UpdateAsync(source);

            return ObjectMapper.Map<RestaurantOrderDto>(subOrder);
        }

        private static string GenerateOrderNumber()
        {
            var now = DateTime.UtcNow;
            var rand = new Random().Next(100, 999);
            return $"ORD-{now:yyMMddHHmm}-{rand}";
        }
    }
}
