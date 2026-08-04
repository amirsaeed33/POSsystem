using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using SmartPos.Inventory;

namespace SmartPos.Inventory.Dto
{
    public class CreateStockAdjustmentDto
    {
        public DateTime AdjustmentDate { get; set; }

        public int Reason { get; set; } = StockAdjustmentReasons.Other;

        [StringLength(StockAdjustment.MaxNotesLength)]
        public string Notes { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreateStockAdjustmentLineDto> Lines { get; set; }
    }

    public class CreateStockAdjustmentLineDto
    {
        [Required]
        public int ProductId { get; set; }

        /// <summary>Positive increases stock; negative decreases stock. Must not be zero.</summary>
        public decimal QuantityChange { get; set; }
    }

    public class StockAdjustmentDto : EntityDto
    {
        public DateTime AdjustmentDate { get; set; }

        public int Reason { get; set; }

        public string ReferenceNo { get; set; }

        public string Notes { get; set; }

        public List<StockAdjustmentLineDto> Lines { get; set; }
    }

    public class StockAdjustmentLineDto : EntityDto
    {
        public int StockAdjustmentId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal QuantityChange { get; set; }
    }

    public class PagedStockAdjustmentResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }
    }
}
