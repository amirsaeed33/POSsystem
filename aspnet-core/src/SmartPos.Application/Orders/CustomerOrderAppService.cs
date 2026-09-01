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
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Customers;
using SmartPos.Inventory;
using SmartPos.Orders.Dto;
using SmartPos.Products;
using SmartPos.Sales;
using SmartPos.Sales.Dto;

namespace SmartPos.Orders
{
    [AbpAuthorize(PermissionNames.Pages_CustomerOrders)]
    public class CustomerOrderAppService :
        AsyncCrudAppService<CustomerOrder, CustomerOrderDto, int, PagedCustomerOrderResultRequestDto, CreateCustomerOrderDto, CustomerOrderDto>,
        ICustomerOrderAppService
    {
        private readonly IRepository<CustomerOrderLine> _lineRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<BranchStock> _branchStockRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;
        private readonly ISaleAppService _saleAppService;

        public CustomerOrderAppService(
            IRepository<CustomerOrder> repository,
            IRepository<CustomerOrderLine> lineRepository,
            IRepository<Product> productRepository,
            IRepository<Customer> customerRepository,
            IRepository<User, long> userRepository,
            IRepository<Branch> branchRepository,
            IRepository<BranchStock> branchStockRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext,
            ISaleAppService saleAppService)
            : base(repository)
        {
            _lineRepository = lineRepository;
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _branchRepository = branchRepository;
            _branchStockRepository = branchStockRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
            _saleAppService = saleAppService;
        }

        [AbpAllowAnonymous]
        public async Task<List<OnlineProductDto>> GetOnlineCatalogAsync(int? branchId)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                Branch targetBranch = null;
                if (branchId.HasValue && branchId.Value > 0)
                {
                    targetBranch = await _branchRepository.FirstOrDefaultAsync(b => b.Id == branchId.Value);
                }

                if (targetBranch == null)
                {
                    targetBranch = await _branchRepository.FirstOrDefaultAsync(b => b.IsActive);
                }

                if (targetBranch == null)
                {
                    return new List<OnlineProductDto>();
                }

                var targetBranchId = targetBranch.Id;

                var products = await _productRepository.GetAllIncluding(p => p.Category, p => p.Unit)
                    .AsNoTracking()
                    .Where(s=>s.BranchId== targetBranchId)
                    .ToListAsync();

                var branchInfo = await _branchStockRepository.GetAll()
                    .Where(s => s.BranchId == targetBranchId)
                    .ToDictionaryAsync(s => s.ProductId, s => new { s.Quantity, s.Price });

                return products
                    .Select(p => new OnlineProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        CategoryName = p.Category?.Name ?? "General",
                        UnitName = p.Unit?.Name ?? "Pcs",
                        Price = branchInfo.TryGetValue(p.Id, out var info) && info.Price > 0 ? info.Price : p.Price,
                        ImagePath = p.ImagePath,
                        InStock = branchInfo.TryGetValue(p.Id, out info) ? info.Quantity > 0 : true
                    })
                    .ToList();
            }
        }

        [AbpAllowAnonymous]
        public async Task<OnlineStoreHeaderDto> GetOnlineStoreHeaderAsync(int branchId)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var branch = await _branchRepository.FirstOrDefaultAsync(b => b.Id == branchId);
                if (branch == null)
                {
                    return new OnlineStoreHeaderDto
                    {
                        BranchId = branchId,
                        BranchName = $"Branch #{branchId}"
                    };
                }

                return new OnlineStoreHeaderDto
                {
                    BranchId = branch.Id,
                    BranchName = branch.Name,
                    Address = branch.InvoiceAddress,
                    Phone = branch.InvoiceContactPhone
                };
            }
        }

        [AbpAllowAnonymous]
        public async Task<CustomerOrderDto> CreateOnlineOrderAsync(CreateCustomerOrderDto input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                if (input.Lines == null || !input.Lines.Any())
                {
                    throw new UserFriendlyException("Add at least one product line.");
                }

                int branchId;
                Branch branch = null;
                if (input.BranchId.HasValue && input.BranchId.Value > 0)
                {
                    branch = await _branchRepository.FirstOrDefaultAsync(b => b.Id == input.BranchId.Value);
                }

                if (branch == null)
                {
                    branch = await _branchRepository.FirstOrDefaultAsync(b => b.IsActive);
                    if (branch == null)
                    {
                        throw new UserFriendlyException("No active location available for online ordering.");
                    }
                }
                branchId = branch.Id;
                var tenantId = branch.TenantId ?? AbpSession.TenantId;

                int customerId = input.CustomerId;
                if (customerId <= 0)
                {
                    var name = string.IsNullOrWhiteSpace(input.CustomerName) ? "Online Customer" : input.CustomerName.Trim();
                    var mobile = string.IsNullOrWhiteSpace(input.CustomerMobile) ? "" : input.CustomerMobile.Trim();

                    Customer existingCustomer = null;
                    if (!string.IsNullOrEmpty(mobile))
                    {
                        existingCustomer = await _customerRepository.FirstOrDefaultAsync(c => c.Phone == mobile && c.BranchId == branchId);
                    }

                    if (existingCustomer != null)
                    {
                        customerId = existingCustomer.Id;
                    }
                    else
                    {
                        var newCust = new Customer
                        {
                            TenantId = tenantId,
                            BranchId = branchId,
                            Name = name,
                            Phone = mobile
                        };
                        customerId = await _customerRepository.InsertAndGetIdAsync(newCust);
                    }
                }

                var order = new CustomerOrder
                {
                    TenantId = tenantId,
                    BranchId = branchId,
                    CustomerId = customerId,
                    OrderDate = input.OrderDate == default ? Abp.Timing.Clock.Now : input.OrderDate,
                    Notes = input.Notes,
                    Status = CustomerOrderStatus.Pending,
                    Lines = new List<CustomerOrderLine>()
                };

                decimal total = 0;
                foreach (var lineInput in input.Lines)
                {
                    if (lineInput.Quantity <= 0)
                    {
                        throw new UserFriendlyException("Quantity must be greater than zero.");
                    }

                    var product = await _productRepository.GetAsync(lineInput.ProductId);
                    var unitPrice = lineInput.UnitPrice > 0 ? lineInput.UnitPrice : product.Price;
                    var lineTotal = lineInput.Quantity * unitPrice;
                    total += lineTotal;

                    order.Lines.Add(new CustomerOrderLine
                    {
                        TenantId = tenantId,
                        ProductId = lineInput.ProductId,
                        Quantity = lineInput.Quantity,
                        UnitPrice = unitPrice,
                        LineTotal = lineTotal
                    });
                }

                order.TotalAmount = total;
                var insertedId = await Repository.InsertAndGetIdAsync(order);
                await CurrentUnitOfWork.SaveChangesAsync();

                order.OrderNo = "ORD-" + insertedId.ToString("D5");
                await CurrentUnitOfWork.SaveChangesAsync();

                return new CustomerOrderDto
                {
                    Id = insertedId,
                    OrderNo = order.OrderNo,
                    CustomerName = input.CustomerName,
                    OrderDate = order.OrderDate,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    Notes = order.Notes
                };
            }
        }

        public override async Task<CustomerOrderDto> CreateAsync(CreateCustomerOrderDto input)
        {
            CheckCreatePermission();

            if (input.Lines == null || !input.Lines.Any())
            {
                throw new UserFriendlyException("Add at least one product line.");
            }

            var customer = await _customerRepository.GetAsync(input.CustomerId);

            if (input.OrderDate == default)
            {
                input.OrderDate = Abp.Timing.Clock.Now;
            }

            var branchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();
            if (customer.BranchId != branchId)
            {
                throw new UserFriendlyException("Customer does not belong to the current location.");
            }

            var order = new CustomerOrder
            {
                TenantId = AbpSession.TenantId,
                BranchId = branchId,
                CustomerId = input.CustomerId,
                OrderDate = input.OrderDate,
                Notes = input.Notes,
                Status = CustomerOrderStatus.Pending,
                Lines = new List<CustomerOrderLine>()
            };

            decimal total = 0;
            foreach (var lineInput in input.Lines)
            {
                if (lineInput.Quantity <= 0)
                {
                    throw new UserFriendlyException("Quantity must be greater than zero.");
                }

                await _productRepository.GetAsync(lineInput.ProductId);

                var lineTotal = lineInput.Quantity * lineInput.UnitPrice;
                total += lineTotal;

                order.Lines.Add(new CustomerOrderLine
                {
                    ProductId = lineInput.ProductId,
                    Quantity = lineInput.Quantity,
                    UnitPrice = lineInput.UnitPrice,
                    LineTotal = lineTotal
                });
            }

            order.TotalAmount = total;

            await Repository.InsertAsync(order);
            await CurrentUnitOfWork.SaveChangesAsync();

            order.OrderNo = "ORD-" + order.Id.ToString("D6");

            return await GetAsync(new EntityDto<int>(order.Id));
        }

        public override async Task<CustomerOrderDto> UpdateAsync(CustomerOrderDto input)
        {
            throw new UserFriendlyException("Orders cannot be edited. Reject and create a new order instead.");
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var order = await GetEntityByIdAsync(input.Id);
            if (order == null)
            {
                throw new UserFriendlyException("Order not found.");
            }

            if (order.Status != CustomerOrderStatus.Pending)
            {
                throw new UserFriendlyException("Only pending orders can be deleted.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

            foreach (var line in order.Lines.ToList())
            {
                await _lineRepository.DeleteAsync(line);
            }

            await Repository.DeleteAsync(order);
        }

        public async Task<SaleDto> ApproveAsync(EntityDto<int> input)
        {
            var order = await GetEntityByIdAsync(input.Id);
            if (order == null)
            {
                throw new UserFriendlyException("Order not found.");
            }

            if (order.Status != CustomerOrderStatus.Pending)
            {
                throw new UserFriendlyException("Only pending orders can be approved.");
            }

            if (order.Lines == null || !order.Lines.Any())
            {
                throw new UserFriendlyException("Order has no product lines.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

            var sale = await _saleAppService.CreateSaleInternalAsync(new CreateSaleDto
            {
                CustomerId = order.CustomerId,
                SaleDate = Abp.Timing.Clock.Now,
                PaymentType = PaymentTypes.Credit,
                Notes = string.IsNullOrWhiteSpace(order.Notes)
                    ? "From order " + (order.OrderNo ?? ("#" + order.Id))
                    : order.Notes + " (Order " + (order.OrderNo ?? ("#" + order.Id)) + ")",
                Lines = order.Lines.Select(x => new CreateSaleLineDto
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice
                }).ToList()
            });

            order.Status = CustomerOrderStatus.Approved;
            order.SaleId = sale.Id;
            await CurrentUnitOfWork.SaveChangesAsync();

            return sale;
        }

        public async Task RejectAsync(EntityDto<int> input)
        {
            var order = await GetEntityByIdAsync(input.Id);
            if (order == null)
            {
                throw new UserFriendlyException("Order not found.");
            }

            if (order.Status != CustomerOrderStatus.Pending)
            {
                throw new UserFriendlyException("Only pending orders can be rejected.");
            }

            await _branchAccessChecker.EnsureCanAccessBranchAsync(order.BranchId);

            order.Status = CustomerOrderStatus.Rejected;
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        protected override IQueryable<CustomerOrder> CreateFilteredQuery(PagedCustomerOrderResultRequestDto input)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);

            return Repository.GetAllIncluding(x => x.Customer, x => x.Sale, x => x.Lines)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.OrderNo != null && x.OrderNo.Contains(input.Keyword))
                         || (x.Notes != null && x.Notes.Contains(input.Keyword))
                         || (x.Customer != null && x.Customer.Name.Contains(input.Keyword))
                         || (x.Sale != null && x.Sale.InvoiceNo != null && x.Sale.InvoiceNo.Contains(input.Keyword)));
        }

        protected override async Task<CustomerOrder> GetEntityByIdAsync(int id)
        {
            return await AsyncQueryableExecuter.FirstOrDefaultAsync(
                Repository.GetAllIncluding(x => x.Customer, x => x.Sale, x => x.Lines)
                    .Where(x => x.Id == id));
        }

        public override async Task<CustomerOrderDto> GetAsync(EntityDto<int> input)
        {
            var entity = await GetEntityByIdAsync(input.Id);
            if (entity == null)
            {
                throw new UserFriendlyException("Order not found.");
            }

            foreach (var line in entity.Lines)
            {
                line.Product = await _productRepository.FirstOrDefaultAsync(line.ProductId);
            }

            return MapToEntityDto(entity);
        }

        protected override CustomerOrderDto MapToEntityDto(CustomerOrder entity)
        {
            var dto = new CustomerOrderDto
            {
                Id = entity.Id,
                CustomerId = entity.CustomerId,
                CustomerName = entity.Customer?.Name,
                OrderDate = entity.OrderDate,
                OrderNo = entity.OrderNo,
                Status = entity.Status,
                StatusName = entity.Status.ToString(),
                TotalAmount = entity.TotalAmount,
                Notes = entity.Notes,
                SaleId = entity.SaleId,
                SaleInvoiceNo = entity.Sale?.InvoiceNo,
                Lines = new List<CustomerOrderLineDto>()
            };

            if (entity.Lines != null)
            {
                foreach (var line in entity.Lines)
                {
                    dto.Lines.Add(new CustomerOrderLineDto
                    {
                        Id = line.Id,
                        OrderId = line.OrderId,
                        ProductId = line.ProductId,
                        ProductName = line.Product?.Name,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        LineTotal = line.LineTotal
                    });
                }
            }

            return dto;
        }
    }
}
