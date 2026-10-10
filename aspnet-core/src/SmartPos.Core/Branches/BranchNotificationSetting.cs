using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Branches
{
    [Table("AppBranchNotificationSettings")]
    public class BranchNotificationSetting : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxEmailsLength = 1000;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        public virtual bool IsEnabled { get; set; }

        public virtual bool NotifyOnSale { get; set; }

        public virtual bool NotifyOnPurchase { get; set; }

        public virtual bool NotifyOnOnlineOrder { get; set; }

        public virtual bool NotifyOnExpense { get; set; }

        [StringLength(MaxEmailsLength)]
        public virtual string Emails { get; set; }
    }
}
