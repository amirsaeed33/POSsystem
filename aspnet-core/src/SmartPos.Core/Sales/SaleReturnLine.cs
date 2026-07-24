using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Products;

namespace SmartPos.Sales
{
    [Table("AppSaleReturnLines")]
    public class SaleReturnLine : FullAuditedEntity, IMayHaveTenant
    {
        public virtual int? TenantId { get; set; }

        [Required]
        public virtual int SaleReturnId { get; set; }

        [ForeignKey(nameof(SaleReturnId))]
        public virtual SaleReturn SaleReturn { get; set; }

        [Required]
        public virtual int SaleLineId { get; set; }

        [ForeignKey(nameof(SaleLineId))]
        public virtual SaleLine SaleLine { get; set; }

        [Required]
        public virtual int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public virtual decimal Quantity { get; set; }

        public virtual decimal UnitPrice { get; set; }

        public virtual decimal LineTotal { get; set; }
    }
}
