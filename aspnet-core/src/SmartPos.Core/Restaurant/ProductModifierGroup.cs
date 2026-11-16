using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Products;

namespace SmartPos.Restaurant
{
    [Table("AppProductModifierGroups")]
    public class ProductModifierGroup : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;

        public virtual int? TenantId { get; set; }

        public virtual int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        public virtual int MinSelection { get; set; } = 0;

        public virtual int MaxSelection { get; set; } = 1;

        public virtual bool IsRequired { get; set; } = false;

        public virtual ICollection<ProductModifierOption> Options { get; set; } = new List<ProductModifierOption>();
    }
}
