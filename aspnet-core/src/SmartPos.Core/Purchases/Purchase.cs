using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Suppliers;

namespace SmartPos.Purchases
{
    [Table("AppPurchases")]
    public class Purchase : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxInvoiceNoLength = 64;
        public const int MaxNotesLength = 512;

        public virtual int? TenantId { get; set; }

        [Required]
        public virtual int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier Supplier { get; set; }

        public virtual DateTime PurchaseDate { get; set; }

        [StringLength(MaxInvoiceNoLength)]
        public virtual string InvoiceNo { get; set; }

        public virtual decimal TotalAmount { get; set; }

        [StringLength(MaxNotesLength)]
        public virtual string Notes { get; set; }

        public virtual ICollection<PurchaseLine> Lines { get; set; }
    }
}
