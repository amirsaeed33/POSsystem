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

        /// <summary>Sum of line totals before discount/tax.</summary>
        public virtual decimal SubTotal { get; set; }

        public virtual decimal DiscountAmount { get; set; }

        public virtual decimal DiscountPercent { get; set; }

        public virtual decimal TaxPercent { get; set; }

        public virtual decimal TaxAmount { get; set; }

        /// <summary>Grand total after discount and tax.</summary>
        public virtual decimal TotalAmount { get; set; }

        /// <summary><see cref="PaymentTypes"/> value.</summary>
        public virtual int PaymentType { get; set; } = PaymentTypes.Credit;

        public virtual decimal CashAmount { get; set; }

        public virtual decimal CardAmount { get; set; }

        /// <summary>Amount charged to customer credit (AR).</summary>
        public virtual decimal CreditAmount { get; set; }

        [StringLength(MaxNotesLength)]
        public virtual string Notes { get; set; }

        public virtual ICollection<SaleLine> Lines { get; set; }
    }
}
