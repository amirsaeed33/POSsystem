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

        public virtual int? TenantId { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }

        [StringLength(MaxBarcodeLength)]
        public virtual string Barcode { get; set; }

        public virtual decimal Price { get; set; }

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
