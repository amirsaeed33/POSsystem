using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Billing
{
    /// <summary>
    /// The plan and paid-until date for one business. Host data, not filtered by the tenant session.
    /// </summary>
    [Table("AppTenantSubscriptions")]
    public class TenantSubscription : FullAuditedEntity
    {
        public const int MaxPlanNameLength = 128;

        public virtual int TenantId { get; set; }

        public virtual int? PackageId { get; set; }

        [Required]
        [StringLength(MaxPlanNameLength)]
        public virtual string PlanName { get; set; }

        public virtual BillingCycle Cycle { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public virtual decimal Amount { get; set; }

        public virtual DateTime? PaidUntil { get; set; }
    }
}
