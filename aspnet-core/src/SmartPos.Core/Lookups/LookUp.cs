using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Lookups
{
    [Table("AppLookUps")]
    public class LookUp : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxTypeLength = 64;
        public const int MaxNameLength = 64;
        public const int MaxDisplayNameLength = 128;

        public virtual int? TenantId { get; set; }

        /// <summary>Lookup category, e.g. PaymentMethod, Gender.</summary>
        [Required]
        [StringLength(MaxTypeLength)]
        public virtual string Type { get; set; }

        /// <summary>Stable code stored on entities (e.g. Cash, Male).</summary>
        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [Required]
        [StringLength(MaxDisplayNameLength)]
        public virtual string DisplayName { get; set; }

        public virtual int SortOrder { get; set; }

        public virtual bool IsActive { get; set; } = true;
    }
}
