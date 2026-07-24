using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace SmartPos.Sales.Dto
{
    public class SaleDto : EntityDto
    {
        [Required]
        public int CustomerId { get; set; }

        public string CustomerName { get; set; }

        public DateTime SaleDate { get; set; }

        public string InvoiceNo { get; set; }

        public decimal TotalAmount { get; set; }

        public string Notes { get; set; }

        public List<SaleLineDto> Lines { get; set; }
    }
}
