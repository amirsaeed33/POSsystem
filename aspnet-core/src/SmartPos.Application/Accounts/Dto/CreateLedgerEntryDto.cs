using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Accounts;

namespace SmartPos.Accounts.Dto
{
    [AutoMapTo(typeof(LedgerEntry))]
    public class CreateLedgerEntryDto
    {
        [Required]
        public int AccountId { get; set; }

        public DateTime TransactionDate { get; set; }

        [Required]
        [StringLength(LedgerEntry.MaxVoucherTypeLength)]
        public string VoucherType { get; set; }

        public int? VoucherId { get; set; }

        public decimal Debit { get; set; }

        public decimal Credit { get; set; }

        [StringLength(LedgerEntry.MaxDescriptionLength)]
        public string Description { get; set; }
    }
}
