using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SmartPos.GroceryPurchases;

namespace SmartPos.GroceryPurchases.Dto
{
    [AutoMapTo(typeof(GroceryPurchase))]
    public class UpdateGroceryPurchaseDto : EntityDto
    {
        [Required]
        [StringLength(GroceryPurchase.MaxProductNameLength)]
        public string ProductName { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal PurchasePrice { get; set; }

        [Required]
        [StringLength(GroceryPurchase.MaxUnitLength)]
        public string Unit { get; set; }

        public int? UnitId { get; set; }

        public DateTime PurchaseDate { get; set; }
    }
}
