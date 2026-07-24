using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartPos.Sales;

namespace SmartPos.Sales.Dto
{
    public class CreateSaleReturnDto
    {
        [Required]
        public int SaleId { get; set; }

        public DateTime ReturnDate { get; set; }

        [StringLength(SaleReturn.MaxNotesLength)]
        public string Notes { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreateSaleReturnLineDto> Lines { get; set; }
    }

    public class CreateSaleReturnLineDto
    {
        [Required]
        public int SaleLineId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }
    }
}
