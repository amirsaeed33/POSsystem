using System;
using Abp.Application.Services.Dto;

namespace SmartPos.GroceryPurchases.Dto
{
    public class PagedGroceryPurchaseResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public int? BranchId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
