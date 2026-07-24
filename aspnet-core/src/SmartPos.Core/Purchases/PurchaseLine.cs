using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Products;

namespace SmartPos.Purchases
{
    [Table("AppPurchaseLines")]
    public class PurchaseLine : FullAuditedEntity, IMayHaveTenant
    {
        public virtual int? TenantId { get; set; }

        [Required]
        public virtual int PurchaseId { get; set; }

        [ForeignKey(nameof(PurchaseId))]
        public virtual Purchase Purchase { get; set; }

        [Required]
        public virtual int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public virtual decimal Quantity { get; set; }

        public virtual decimal UnitCost { get; set; }

        public virtual decimal LineTotal { get; set; }
    }
}
