using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Customers;

namespace SmartPos.Sales
{
    [Table("AppSales")]
    public class Sale : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxInvoiceNoLength = 64;
        public const int MaxNotesLength = 512;

        public virtual int? TenantId { get; set; }

        [Required]
        public virtual int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; }

        public virtual DateTime SaleDate { get; set; }

        [StringLength(MaxInvoiceNoLength)]
        public virtual string InvoiceNo { get; set; }

        public virtual decimal TotalAmount { get; set; }

        [StringLength(MaxNotesLength)]
        public virtual string Notes { get; set; }

        public virtual ICollection<SaleLine> Lines { get; set; }
    }
}
