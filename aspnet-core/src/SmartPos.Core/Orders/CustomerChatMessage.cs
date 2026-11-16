using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;

namespace SmartPos.Orders
{
    public enum ChatMessageSenderType
    {
        Customer = 0,
        Store = 1
    }

    [Table("AppCustomerChatMessages")]
    public class CustomerChatMessage : CreationAuditedEntity<long>, IMayHaveTenant
    {
        public const int MaxOrderNoLength = 64;
        public const int MaxSessionIdLength = 128;
        public const int MaxSenderNameLength = 256;
        public const int MaxMobileLength = 64;
        public const int MaxMessageLength = 2000;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        public virtual int? OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual CustomerOrder Order { get; set; }

        [StringLength(MaxOrderNoLength)]
        public virtual string OrderNo { get; set; }

        /// <summary>
        /// Guest or customer session/token ID to group conversations before/during order placement.
        /// </summary>
        [StringLength(MaxSessionIdLength)]
        public virtual string SessionId { get; set; }

        [StringLength(MaxSenderNameLength)]
        public virtual string SenderName { get; set; }

        [StringLength(MaxMobileLength)]
        public virtual string CustomerMobile { get; set; }

        public virtual ChatMessageSenderType SenderType { get; set; }

        [Required]
        [StringLength(MaxMessageLength)]
        public virtual string Message { get; set; }

        public virtual bool IsReadByCustomer { get; set; }

        public virtual bool IsReadByStore { get; set; }
    }
}
