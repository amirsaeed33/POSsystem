using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Accounts
{
    [Table("AppLedgerEntries")]
    public class LedgerEntry : CreationAuditedEntity, IMayHaveTenant
    {
        public const int MaxVoucherTypeLength = 64;
        public const int MaxDescriptionLength = 512;

        public virtual int? TenantId { get; set; }

        [Required]
        public virtual int AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public virtual BusinessAccount Account { get; set; }

        public virtual DateTime TransactionDate { get; set; }

        [Required]
        [StringLength(MaxVoucherTypeLength)]
        public virtual string VoucherType { get; set; }

        public virtual int? VoucherId { get; set; }

        public virtual decimal Debit { get; set; }

        public virtual decimal Credit { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }
    }
}
