using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;
using SmartPos.Units;

namespace SmartPos.Categories
{
    [Table("AppCategories")]
    public class Category : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;
        public const int MaxDescriptionLength = 512;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }

        public virtual bool IsActive { get; set; } = true;

        public virtual int? DefaultUnitId { get; set; }

        [ForeignKey(nameof(DefaultUnitId))]
        public virtual Unit DefaultUnit { get; set; }
    }
}
