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

        /// <summary>
        /// Empty price box on create: show product sale amount, do not add as +extra.
        /// Explicitly typed prices (even equal to sale) are always +extra.
        /// </summary>
        public virtual bool IsIncludedInBasePrice { get; set; } = false;

        public virtual bool IsDefault { get; set; } = false;
    }
}
