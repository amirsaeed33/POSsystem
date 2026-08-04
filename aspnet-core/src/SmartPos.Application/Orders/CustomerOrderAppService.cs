using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using SmartPos.Authorization;
using SmartPos.Customers;
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
        private readonly ISaleAppService _saleAppService;

        public CustomerOrderAppService(
            IRepository<CustomerOrder> repository,
            IRepository<CustomerOrderLine> lineRepository,
            IRepository<Product> productRepository,
            IRepository<Customer> customerRepository,
            ISaleAppService saleAppService)
            : base(repository)
        {
            _lineRepository = lineRepository;
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _saleAppService = saleAppService;
        }

        public override async Task<CustomerOrderDto> CreateAsync(CreateCustomerOrderDto input)
        {
            CheckCreatePermission();

            if (input.Lines == null || !input.Lines.Any())
            {
                throw new UserFriendlyException("Add at least one product line.");
            }

            await _customerRepository.GetAsync(input.CustomerId);

            if (input.OrderDate == default)
            {
                input.OrderDate = Abp.Timing.Clock.Now;
            }

            var order = new CustomerOrder
            {
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

            var sale = await _saleAppService.CreateAsync(new CreateSaleDto
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

            order.Status = CustomerOrderStatus.Rejected;
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        protected override IQueryable<CustomerOrder> CreateFilteredQuery(PagedCustomerOrderResultRequestDto input)
        {
            return Repository.GetAllIncluding(x => x.Customer, x => x.Sale, x => x.Lines)
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
