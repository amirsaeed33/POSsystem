using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;

namespace SmartPos.Staffs
{
    [Table("AppStaffHistories")]
    public class StaffHistory : CreationAuditedEntity, IMayHaveTenant
    {
        public const int MaxActionLength = 64;
        public const int MaxDescriptionLength = 1024;

        public virtual int? TenantId { get; set; }

        public virtual int? BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        public virtual int StaffId { get; set; }

        [ForeignKey(nameof(StaffId))]
        public virtual Staff Staff { get; set; }

        [Required]
        [StringLength(MaxActionLength)]
        public virtual string Action { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }
    }
}
