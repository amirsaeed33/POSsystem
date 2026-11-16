using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;
using SmartPos.Categories;
using SmartPos.Products;

namespace SmartPos.Deals
{
    [Table("AppDeals")]
    public class Deal : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 256;
        public const int MaxDescriptionLength = 1024;
        public const int MaxCategoryTypeLength = 64;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }

        public virtual decimal DealPrice { get; set; }

        public virtual DateTime? ExpiryDate { get; set; }

        [StringLength(MaxCategoryTypeLength)]
        public virtual string CategoryType { get; set; } = "Deal";

        public virtual int? CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category Category { get; set; }

        public virtual bool IsActive { get; set; } = true;

        /// <summary>
        /// Corresponding product entry in AppProducts so the deal can be sold in POS and listed seamlessly.
        /// </summary>
        public virtual int? ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public virtual ICollection<DealDetail> DealDetails { get; set; }

        public Deal()
        {
            DealDetails = new HashSet<DealDetail>();
        }
    }
}
