using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;

namespace SmartPos.Authorization
{
    /// <summary>
    /// One row per user who currently has the app open. Updated by a heartbeat
    /// and cleared on sign-out.
    /// </summary>
    [Table("AppUserPresences")]
    public class UserPresence : Entity<long>, IMayHaveTenant
    {
        public const int MaxNameLength = 256;
        public const int MaxRoleLength = 256;

        public virtual int? TenantId { get; set; }

        public virtual long UserId { get; set; }

        public virtual int? BranchId { get; set; }

        [StringLength(MaxNameLength)]
        public virtual string UserName { get; set; }

        [StringLength(MaxNameLength)]
        public virtual string FullName { get; set; }

        [StringLength(MaxNameLength)]
        public virtual string BranchName { get; set; }

        [StringLength(MaxNameLength)]
        public virtual string TenancyName { get; set; }

        [StringLength(MaxRoleLength)]
        public virtual string RoleNames { get; set; }

        public virtual DateTime LoginTime { get; set; }

        public virtual DateTime LastSeen { get; set; }

        public virtual DateTime? SignedOutTime { get; set; }
    }
}
