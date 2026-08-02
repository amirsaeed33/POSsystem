using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;
using SmartPos.Customers;
using SmartPos.Sales;

namespace SmartPos.Orders
{
    [Table("AppCustomerOrders")]
    public class CustomerOrder : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxOrderNoLength = 64;
        public const int MaxNotesLength = 512;

        public virtual int? TenantId { get; set; }

        [Required]
        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        [Required]
        public virtual int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; }

        public virtual DateTime OrderDate { get; set; }

        [StringLength(MaxOrderNoLength)]
        public virtual string OrderNo { get; set; }

        public virtual CustomerOrderStatus Status { get; set; }

        public virtual decimal TotalAmount { get; set; }

        [StringLength(MaxNotesLength)]
        public virtual string Notes { get; set; }

        public virtual int? SaleId { get; set; }

        [ForeignKey(nameof(SaleId))]
        public virtual Sale Sale { get; set; }

        public virtual ICollection<CustomerOrderLine> Lines { get; set; }
    }
}
