using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Products;

namespace SmartPos.Products.Dto
{
    [AutoMapTo(typeof(Product))]
    public class CreateProductDto
    {
        [Required]
        [StringLength(Product.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Product.MaxDescriptionLength)]
        public string Description { get; set; }

        [StringLength(Product.MaxBarcodeLength)]
        public string Barcode { get; set; }

        public decimal Price { get; set; }

        public decimal AlertQuantityLimit { get; set; } = 10;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int BrandId { get; set; }

        [Required]
        public int UnitId { get; set; }

        public string ImageBase64 { get; set; }
    }
}
