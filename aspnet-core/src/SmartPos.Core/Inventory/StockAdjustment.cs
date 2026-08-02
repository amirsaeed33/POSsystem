using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;

namespace SmartPos.Inventory
{
    [Table("AppStockAdjustments")]
    public class StockAdjustment : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNotesLength = 512;
        public const int MaxReferenceNoLength = 64;

        public virtual int? TenantId { get; set; }

        [Required]
        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        public virtual DateTime AdjustmentDate { get; set; }

        public virtual int Reason { get; set; } = StockAdjustmentReasons.Other;

        [StringLength(MaxReferenceNoLength)]
        public virtual string ReferenceNo { get; set; }

        [StringLength(MaxNotesLength)]
        public virtual string Notes { get; set; }

        public virtual ICollection<StockAdjustmentLine> Lines { get; set; }
    }
}
