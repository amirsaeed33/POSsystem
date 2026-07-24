using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Accounts;

namespace SmartPos.Expenses
{
    [Table("AppExpenses")]
    public class Expense : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxReferenceNoLength = 64;
        public const int MaxDescriptionLength = 512;

        public virtual int? TenantId { get; set; }

        public virtual DateTime ExpenseDate { get; set; }

        public virtual decimal Amount { get; set; }

        [StringLength(MaxReferenceNoLength)]
        public virtual string ReferenceNo { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }

        [Required]
        public virtual int PaymentAccountId { get; set; }

        [ForeignKey(nameof(PaymentAccountId))]
        public virtual BusinessAccount PaymentAccount { get; set; }

        [Required]
        public virtual int ExpenseAccountId { get; set; }

        [ForeignKey(nameof(ExpenseAccountId))]
        public virtual BusinessAccount ExpenseAccount { get; set; }
    }
}
