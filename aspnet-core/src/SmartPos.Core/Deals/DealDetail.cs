using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Products;

namespace SmartPos.Deals
{
    [Table("AppDealDetails")]
    public class DealDetail : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxSelectedModifiersLength = 1024;
        public const int MaxSelectedOptionIdsLength = 512;

        public virtual int? TenantId { get; set; }

        public virtual int DealId { get; set; }

        [ForeignKey(nameof(DealId))]
        public virtual Deal Deal { get; set; }

        public virtual int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public virtual decimal Quantity { get; set; } = 1;

        public virtual decimal UnitPrice { get; set; }

        /// <summary>Human-readable options chosen when building the deal (e.g. "Size: Small").</summary>
        [StringLength(MaxSelectedModifiersLength)]
        public virtual string SelectedModifiers { get; set; }

        /// <summary>Comma-separated modifier option ids selected for this deal line.</summary>
        [StringLength(MaxSelectedOptionIdsLength)]
        public virtual string SelectedOptionIds { get; set; }
    }
}
