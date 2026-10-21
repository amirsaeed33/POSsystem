using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;

namespace SmartPos.Orders
{
    [Table("AppDeliveryAreas")]
    public class DeliveryArea : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;
        public const int MaxEstimatedTimeLength = 64;
        public const int MaxCoverageDetailsLength = 256;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public virtual decimal DeliveryFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public virtual decimal MinimumOrderAmount { get; set; }

        [StringLength(MaxEstimatedTimeLength)]
        public virtual string EstimatedDeliveryTime { get; set; }

        [StringLength(MaxCoverageDetailsLength)]
        public virtual string CoverageDetails { get; set; }

        public virtual bool IsActive { get; set; } = true;
    }
}
