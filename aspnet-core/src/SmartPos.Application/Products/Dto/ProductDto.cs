using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using SmartPos.Products;

namespace SmartPos.Products.Dto
{
    public class ProductDto : EntityDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(Product.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Product.MaxDescriptionLength)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Barcode is required.")]
        [StringLength(Product.MaxBarcodeLength)]
        public string Barcode { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price is required.")]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Wholesale price is required.")]
        public decimal WholesalePrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cost price is required.")]
        public decimal CostPrice { get; set; }

        /// <summary>Selling price minus cost price (per unit).</summary>
        public decimal ProfitPerUnit { get; set; }

        /// <summary>Profit as percent of selling price.</summary>
        public decimal? ProfitMarginPercent { get; set; }

        /// <summary>Potential profit if current stock is sold at Price.</summary>
        public decimal StockProfit { get; set; }

        public decimal StockQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Alert quantity limit is required.")]
        public decimal AlertQuantityLimit { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }

        [Required(ErrorMessage = "Brand is required.")]
        public int BrandId { get; set; }

        public string BrandName { get; set; }

        [Required(ErrorMessage = "Unit is required.")]
        public int UnitId { get; set; }

        public string UnitName { get; set; }

        public string ImagePath { get; set; }

        public string ImageBase64 { get; set; }
    }
}
