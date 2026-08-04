using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;
using SmartPos.Products;

namespace SmartPos.Inventory
{
    [Table("AppBranchStocks")]
    public class BranchStock : FullAuditedEntity, IMayHaveTenant
    {
        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        public virtual int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public virtual decimal Quantity { get; set; }

        /// <summary>
        /// Branch retail / direct-customer selling price per unit.
        /// </summary>
        public virtual decimal Price { get; set; }

        /// <summary>
        /// Branch wholesale selling price per unit.
        /// </summary>
        public virtual decimal WholesalePrice { get; set; }

        /// <summary>
        /// Branch average purchase / cost price per unit.
        /// </summary>
        public virtual decimal CostPrice { get; set; }
    }
}
