using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Authorization.Users;

namespace SmartPos.Branches
{
    [Table("AppUserBranches")]
    public class UserBranch : CreationAuditedEntity, IMayHaveTenant
    {
        public virtual int? TenantId { get; set; }

        public virtual long UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }
    }
}
