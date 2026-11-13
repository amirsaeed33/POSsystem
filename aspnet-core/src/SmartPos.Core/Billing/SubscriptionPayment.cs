using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Billing
{
    /// <summary>
    /// A receipt the business uploaded. The paid-until date changes only after the host approves it.
    /// </summary>
    [Table("AppSubscriptionPayments")]
    public class SubscriptionPayment : FullAuditedEntity
    {
        public const int MaxPlanNameLength = 128;
        public const int MaxReviewNoteLength = 1000;
        public const int MaxContentTypeLength = 64;
        public const int MaxFileNameLength = 256;
        public const int MaxApprovalTokenLength = 64;

        public virtual int TenantId { get; set; }

        public virtual int? PackageId { get; set; }

        [Required]
        [StringLength(MaxPlanNameLength)]
        public virtual string PlanName { get; set; }

        public virtual BillingCycle Cycle { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public virtual decimal Amount { get; set; }

        public virtual PaymentMethodKind Method { get; set; }

        public virtual int? PaymentMethodId { get; set; }

        public virtual SubscriptionPaymentStatus Status { get; set; }

        public virtual byte[] ReceiptImage { get; set; }

        [StringLength(MaxContentTypeLength)]
        public virtual string ReceiptContentType { get; set; }

        [StringLength(MaxFileNameLength)]
        public virtual string ReceiptFileName { get; set; }

        [StringLength(MaxReviewNoteLength)]
        public virtual string ReviewNote { get; set; }

        public virtual DateTime? ReviewedTime { get; set; }

        public virtual long? ReviewedByUserId { get; set; }

        /// <summary>Secret used by the one-click approve link in the host email.</summary>
        [StringLength(MaxApprovalTokenLength)]
        public virtual string ApprovalToken { get; set; }
    }
}
