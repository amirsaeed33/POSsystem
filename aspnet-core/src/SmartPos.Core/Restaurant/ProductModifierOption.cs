using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Restaurant
{
    [Table("AppProductModifierOptions")]
    public class ProductModifierOption : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;

        public virtual int? TenantId { get; set; }

        public virtual int ModifierGroupId { get; set; }

        [ForeignKey(nameof(ModifierGroupId))]
        public virtual ProductModifierGroup ModifierGroup { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        public virtual decimal Price { get; set; } = 0;

        public virtual bool IsDefault { get; set; } = false;
    }
}
