using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Accounts;

namespace SmartPos.Accounts.Dto
{
    [AutoMapTo(typeof(BusinessAccount))]
    public class CreateBusinessAccountDto
    {
        [Required]
        [StringLength(BusinessAccount.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(BusinessAccount.MaxCodeLength)]
        public string Code { get; set; }

        [StringLength(BusinessAccount.MaxAccountTypeLength)]
        public string AccountType { get; set; }

        public int? AccountTypeId { get; set; }

        public decimal OpeningBalance { get; set; }

        [StringLength(BusinessAccount.MaxDescriptionLength)]
        public string Description { get; set; }

        public bool IsActive { get; set; }
    }
}
