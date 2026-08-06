using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Products;

namespace SmartPos.Products.Dto
{
    [AutoMapTo(typeof(Product))]
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(Product.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Product.MaxDescriptionLength)]
        public string Description { get; set; }

        [StringLength(Product.MaxLocationLength)]
        public string Location { get; set; }

        [Required(ErrorMessage = "Barcode is required.")]
        [StringLength(Product.MaxBarcodeLength)]
        public string Barcode { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price is required.")]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Wholesale price is required.")]
        public decimal WholesalePrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cost price is required.")]
        public decimal CostPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Alert quantity limit is required.")]
        public decimal AlertQuantityLimit { get; set; } = 10;

        [Range(0, double.MaxValue)]
        public decimal StockQuantity { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Brand is required.")]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Unit is required.")]
        public int UnitId { get; set; }

        public string ImageBase64 { get; set; }

        /// <summary>
        /// Tenant-admin only. Optional. Empty = tenant-level (all locations);
        /// otherwise product is limited to these branches.
        /// </summary>
        public List<int> BranchIds { get; set; }
    }
}
