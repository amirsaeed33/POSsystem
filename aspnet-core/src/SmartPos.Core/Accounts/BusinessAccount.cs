using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Lookups;

namespace SmartPos.Accounts
{
    [Table("AppAccounts")]
    public class BusinessAccount : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;
        public const int MaxCodeLength = 64;
        public const int MaxAccountTypeLength = 64;
        public const int MaxDescriptionLength = 512;

        public virtual int? TenantId { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [StringLength(MaxCodeLength)]
        public virtual string Code { get; set; }

        [StringLength(MaxAccountTypeLength)]
        public virtual string AccountType { get; set; }

        /// <summary>
        /// FK to <see cref="LookUp"/> where Type = AccountType.
        /// </summary>
        public virtual int? AccountTypeId { get; set; }

        [ForeignKey(nameof(AccountTypeId))]
        public virtual LookUp AccountTypeLookup { get; set; }

        public virtual decimal OpeningBalance { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }

        public virtual bool IsActive { get; set; }
    }
}
