using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Purchases
{
    [Table("AppPurchaseReturns")]
    public class PurchaseReturn : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNotesLength = 512;

        public virtual int? TenantId { get; set; }

        [Required]
        public virtual int PurchaseId { get; set; }

        [ForeignKey(nameof(PurchaseId))]
        public virtual Purchase Purchase { get; set; }

        public virtual DateTime ReturnDate { get; set; }

        public virtual decimal TotalAmount { get; set; }

        [StringLength(MaxNotesLength)]
        public virtual string Notes { get; set; }

        public virtual ICollection<PurchaseReturnLine> Lines { get; set; }
    }
}
