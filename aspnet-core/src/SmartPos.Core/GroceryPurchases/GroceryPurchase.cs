using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;
using SmartPos.Units;

namespace SmartPos.GroceryPurchases
{
    [Table("AppGroceryPurchases")]
    public class GroceryPurchase : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxProductNameLength = 256;
        public const int MaxUnitLength = 64;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        [Required]
        [StringLength(MaxProductNameLength)]
        public virtual string ProductName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public virtual decimal PurchasePrice { get; set; }

        [Required]
        [StringLength(MaxUnitLength)]
        public virtual string Unit { get; set; }

        public virtual int? UnitId { get; set; }

        [ForeignKey(nameof(UnitId))]
        public virtual Unit UnitRef { get; set; }

        public virtual DateTime PurchaseDate { get; set; }
    }
}
