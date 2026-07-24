using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Products;

namespace SmartPos.Orders
{
    [Table("AppCustomerOrderLines")]
    public class CustomerOrderLine : FullAuditedEntity, IMayHaveTenant
    {
        public virtual int? TenantId { get; set; }

        [Required]
        public virtual int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual CustomerOrder Order { get; set; }

        [Required]
        public virtual int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public virtual decimal Quantity { get; set; }

        public virtual decimal UnitPrice { get; set; }

        public virtual decimal LineTotal { get; set; }
    }
}
