using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Products;

namespace SmartPos.Inventory
{
    [Table("AppStockAdjustmentLines")]
    public class StockAdjustmentLine : FullAuditedEntity, IMayHaveTenant
    {
        public virtual int? TenantId { get; set; }

        public virtual int StockAdjustmentId { get; set; }

        [ForeignKey(nameof(StockAdjustmentId))]
        public virtual StockAdjustment StockAdjustment { get; set; }

        public virtual int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        /// <summary>Positive increases stock; negative decreases stock.</summary>
        public virtual decimal QuantityChange { get; set; }
    }
}
