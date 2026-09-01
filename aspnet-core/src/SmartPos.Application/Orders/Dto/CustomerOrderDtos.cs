using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using SmartPos.Orders;

namespace SmartPos.Orders.Dto
{
    public class CreateCustomerOrderDto
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerMobile { get; set; }

        public int? BranchId { get; set; }

        public DateTime OrderDate { get; set; }

        [StringLength(CustomerOrder.MaxNotesLength)]
        public string Notes { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreateCustomerOrderLineDto> Lines { get; set; }
    }

    public class OnlineStoreHeaderDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
    }

    public class OnlineProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string UnitName { get; set; }
        public decimal Price { get; set; }
        public string ImagePath { get; set; }
        public bool InStock { get; set; }
    }

    public class CreateCustomerOrderLineDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }

    public class CustomerOrderLineDto : EntityDto
    {
        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }
    }

    public class CustomerOrderDto : EntityDto
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; }

        public DateTime OrderDate { get; set; }

        public string OrderNo { get; set; }

        public CustomerOrderStatus Status { get; set; }

        public string StatusName { get; set; }

        public decimal TotalAmount { get; set; }

        public string Notes { get; set; }

        public int? SaleId { get; set; }

        public string SaleInvoiceNo { get; set; }

        public List<CustomerOrderLineDto> Lines { get; set; }
    }

    public class PagedCustomerOrderResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public CustomerOrderStatus? Status { get; set; }
    }
}
