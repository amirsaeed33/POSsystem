using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace SmartPos.Deals.Dto
{
    public class DealDto : EntityDto
    {
        public int BranchId { get; set; }

        public string BranchName { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal DealPrice { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;

        public string CategoryType { get; set; }

        public int? CategoryId { get; set; }

        public string CategoryName { get; set; }

        public bool IsActive { get; set; }

        public int? ProductId { get; set; }

        public List<DealDetailDto> Items { get; set; } = new List<DealDetailDto>();

        public decimal TotalOriginalPrice
        {
            get
            {
                decimal total = 0;
                if (Items != null)
                {
                    foreach (var item in Items)
                    {
                        total += item.SubTotal;
                    }
                }
                return total;
            }
        }
    }
}
