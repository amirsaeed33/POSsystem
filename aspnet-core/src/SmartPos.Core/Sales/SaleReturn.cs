using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Sales
{
    [Table("AppSaleReturns")]
    public class SaleReturn : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNotesLength = 512;

        public virtual int? TenantId { get; set; }

        [Required]
        public virtual int SaleId { get; set; }

        [ForeignKey(nameof(SaleId))]
        public virtual Sale Sale { get; set; }

        public virtual DateTime ReturnDate { get; set; }

        public virtual decimal TotalAmount { get; set; }

        [StringLength(MaxNotesLength)]
        public virtual string Notes { get; set; }

        public virtual ICollection<SaleReturnLine> Lines { get; set; }
    }
}
