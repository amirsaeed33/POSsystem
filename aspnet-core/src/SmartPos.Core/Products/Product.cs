using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Brands;
using SmartPos.Categories;
using SmartPos.Units;

namespace SmartPos.Products
{
    [Table("AppProducts")]
    public class Product : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 256;
        public const int MaxDescriptionLength = 1024;
        public const int MaxImagePathLength = 512;
        public const int MaxBarcodeLength = 64;
        public const int MaxLocationLength = 256;

        public virtual int? TenantId { get; set; }

        /// <summary>
        /// When true, the product is available to all branches under the tenant.
        /// When false, visibility is limited to branches that have an AppBranchStocks row.
        /// </summary>
        public virtual bool IsShared { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }

        [StringLength(MaxLocationLength)]
        public virtual string Location { get; set; }

        [StringLength(MaxBarcodeLength)]
        public virtual string Barcode { get; set; }

        /// <summary>
        /// Retail / direct-customer selling price per unit.
        /// </summary>
        public virtual decimal Price { get; set; }

        /// <summary>
        /// Wholesale selling price per unit (used for wholesaler customers).
        /// </summary>
        public virtual decimal WholesalePrice { get; set; }

        /// <summary>
        /// Average purchase / cost price per unit (weighted average on purchases).
        /// </summary>
        public virtual decimal CostPrice { get; set; }

        public virtual decimal StockQuantity { get; set; }

        /// <summary>
        /// Stock is considered low when quantity is greater than 0 and less than or equal to this limit.
        /// </summary>
        public virtual decimal AlertQuantityLimit { get; set; } = 10;

        public virtual int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category Category { get; set; }

        public virtual int BrandId { get; set; }

        [ForeignKey(nameof(BrandId))]
        public virtual Brand Brand { get; set; }

        public virtual int UnitId { get; set; }

        [ForeignKey(nameof(UnitId))]
        public virtual Unit Unit { get; set; }

        [StringLength(MaxImagePathLength)]
        public virtual string ImagePath { get; set; }
    }
}
