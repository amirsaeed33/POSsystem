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

            return ObjectMapper.Map<RestaurantOrderDto>(order);
        }

        public async Task<RestaurantOrderDto> GetByTableAsync(int tableId)
        {
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
                order = new RestaurantOrder
                {
                    BranchId = input.BranchId,
                    OrderNo = GenerateOrderNumber(),
                    TableId = null,
                    TableNo = "Takeaway",
                    Status = RestaurantOrderStatus.Open,
                    OrderType = input.OrderType
                };
                isNew = true;
            }

            order.CustomerId = input.CustomerId;
            order.WaiterName = input.WaiterName;
            if (string.IsNullOrWhiteSpace(order.WaiterName) && order.TableId.HasValue)
            {
                var tbl = await _tableRepository.FirstOrDefaultAsync(order.TableId.Value);
                if (!string.IsNullOrWhiteSpace(tbl?.WaiterName))
                {
                    order.WaiterName = tbl.WaiterName;
                }
            }
            order.Notes = input.Notes;
            order.OrderType = input.OrderType;
            if (!string.IsNullOrWhiteSpace(input.TableNo))
            {
                order.TableNo = input.TableNo;
            }

            decimal subTotal = 0;

            // Remove lines that are no longer in input (only if unprinted)
            var inputLineIds = input.Lines.Where(l => l.Id.HasValue).Select(l => l.Id.Value).ToList();
            var linesToRemove = order.Lines.Where(l => !l.IsSentToKitchen && !inputLineIds.Contains(l.Id)).ToList();
            foreach (var rem in linesToRemove)
            {
                order.Lines.Remove(rem);
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
                        existingLine.Quantity = lDto.Quantity;
                        existingLine.UnitPrice = lDto.UnitPrice;
                        existingLine.LineTotal = lineTotal;
                        existingLine.KitchenNotes = lDto.KitchenNotes;
                        existingLine.SelectedModifiers = lDto.SelectedModifiers;
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
                    IsSentToKitchen = lDto.IsSentToKitchen
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
            order.ServiceChargeAmount = (input.ServiceChargePercent > 0 && input.OrderType == OrderType.DineIn)
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

            var unprintedLines = order.Lines.Where(l => !l.IsSentToKitchen).ToList();
            if (!unprintedLines.Any())
            {
                // If all are already printed, reprint all for reference
                unprintedLines = order.Lines.ToList();
            }
            else
            {
                var now = DateTime.UtcNow;
                foreach (var line in unprintedLines)
                {
                    line.IsSentToKitchen = true;
                    line.SentToKitchenTime = now;
                }
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            var printer = await GetCurrentPrinterIdentityAsync();
            order.LastPrintedBy = $"{printer.Name} ({printer.Role})";
            order.LastPrintedTime = DateTime.UtcNow;
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
                PrintTime = DateTime.UtcNow
            });

            return new SendToKitchenResultDto
            {
                OrderId = order.Id,
                OrderNo = order.OrderNo,
                TableNo = order.TableNo ?? "Takeaway",
                WaiterName = order.WaiterName ?? "Staff",
                PrintTime = DateTime.UtcNow,
                PrintedBy = $"{printer.Name} ({printer.Role})",
                PrintedByUserId = printer.UserId,
                PrintedByName = printer.Name,
                PrintedByRole = printer.Role,
                NewItems = unprintedLines.Select(l => new KitchenSlipItemDto
                {
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
            string tableTag = order.OrderType == OrderType.DineIn
                ? $"[Dine-In - Table {order.TableNo}]{waiterTag}"
                : "[Takeaway]";
            string finalNotes = string.IsNullOrWhiteSpace(input.Notes)
                ? tableTag
                : $"{tableTag} {input.Notes.Trim()}";

            // Settle order via SaleAppService
            var saleDto = new CreateSaleDto
            {
                CustomerId = input.CustomerId,
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
                            o.Lines.Any(l => l.IsSentToKitchen))
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
            var order = await _orderRepository.GetAsync(orderId);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);
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
