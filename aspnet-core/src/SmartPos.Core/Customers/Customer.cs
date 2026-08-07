using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Accounts;
using SmartPos.Branches;

namespace SmartPos.Customers
{
    [Table("AppCustomers")]
    public class Customer : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;
        public const int MaxPhoneLength = 32;
        public const int MaxEmailLength = 256;
        public const int MaxAddressLength = 512;
        public const int MaxDescriptionLength = 512;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        public virtual int? AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public virtual BusinessAccount Account { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        /// <summary>
        /// <see cref="CustomerTypes.Direct"/> or <see cref="CustomerTypes.Wholesaler"/>.
        /// </summary>
        public virtual int CustomerType { get; set; } = CustomerTypes.Direct;

        [StringLength(MaxPhoneLength)]
        public virtual string Phone { get; set; }

        [StringLength(MaxEmailLength)]
        public virtual string Email { get; set; }

        [StringLength(MaxAddressLength)]
        public virtual string Address { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }
    }
}
