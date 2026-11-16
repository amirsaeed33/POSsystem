using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;
using SmartPos.Products;

namespace SmartPos.Orders
{
    [Table("AppDeliveryPromotions")]
    public class DeliveryPromotion : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;
        public const int MaxNotesLength = 256;

        public virtual int? TenantId { get; set; }

        public virtual int? BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public virtual decimal? MinimumOrderAmount { get; set; }

        public virtual int? ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public virtual DateTime? StartDate { get; set; }

        public virtual DateTime? EndDate { get; set; }

        public virtual bool IsActive { get; set; } = true;

        [StringLength(MaxNotesLength)]
        public virtual string Notes { get; set; }
    }
}
