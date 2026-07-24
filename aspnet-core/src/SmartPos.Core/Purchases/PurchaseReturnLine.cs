using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Products;

namespace SmartPos.Purchases
{
    [Table("AppPurchaseReturnLines")]
    public class PurchaseReturnLine : FullAuditedEntity, IMayHaveTenant
    {
        public virtual int? TenantId { get; set; }

        [Required]
        public virtual int PurchaseReturnId { get; set; }

        [ForeignKey(nameof(PurchaseReturnId))]
        public virtual PurchaseReturn PurchaseReturn { get; set; }

        [Required]
        public virtual int PurchaseLineId { get; set; }

        [ForeignKey(nameof(PurchaseLineId))]
        public virtual PurchaseLine PurchaseLine { get; set; }

        [Required]
        public virtual int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public virtual decimal Quantity { get; set; }

        public virtual decimal UnitCost { get; set; }

        public virtual decimal LineTotal { get; set; }
    }
}
