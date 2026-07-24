using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Products;

namespace SmartPos.Sales
{
    [Table("AppSaleLines")]
    public class SaleLine : FullAuditedEntity, IMayHaveTenant
    {
        public virtual int? TenantId { get; set; }

        [Required]
        public virtual int SaleId { get; set; }

        [ForeignKey(nameof(SaleId))]
        public virtual Sale Sale { get; set; }

        [Required]
        public virtual int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public virtual decimal Quantity { get; set; }

        public virtual decimal UnitPrice { get; set; }

        public virtual decimal LineTotal { get; set; }
    }
}
