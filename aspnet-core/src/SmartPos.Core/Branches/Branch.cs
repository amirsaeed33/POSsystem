using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Branches
{
    [Table("AppBranches")]
    public class Branch : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;
        public const int MaxCodeLength = 32;
        public const string DefaultBranchCode = "DEFAULT";
        public const string DefaultBranchName = "Main";

        public virtual int? TenantId { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [Required]
        [StringLength(MaxCodeLength)]
        public virtual string Code { get; set; }

        public virtual bool IsActive { get; set; } = true;

        public virtual bool IsDefault { get; set; }
    }
}
