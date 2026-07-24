using System;
using Abp.Application.Services.Dto;

namespace SmartPos.Accounts.Dto
{
    public class PagedLedgerEntryResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public int? AccountId { get; set; }

        public string VoucherType { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
