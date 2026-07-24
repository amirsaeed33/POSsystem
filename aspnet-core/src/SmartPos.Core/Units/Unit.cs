using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Units
{
    [Table("AppUnits")]
    public class Unit : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;
        public const int MaxDescriptionLength = 512;

        public virtual int? TenantId { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }
    }
}
