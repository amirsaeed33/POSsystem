using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using SmartPos.Products;

namespace SmartPos.Products.Dto
{
    public class ProductDto : EntityDto
    {
        [Required]
        [StringLength(Product.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Product.MaxDescriptionLength)]
        public string Description { get; set; }

        [StringLength(Product.MaxBarcodeLength)]
        public string Barcode { get; set; }

        public decimal Price { get; set; }

        public decimal StockQuantity { get; set; }

        public decimal AlertQuantityLimit { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }

        [Required]
        public int BrandId { get; set; }

        public string BrandName { get; set; }

        [Required]
        public int UnitId { get; set; }

        public string UnitName { get; set; }

        public string ImagePath { get; set; }

        public string ImageBase64 { get; set; }
    }
}
