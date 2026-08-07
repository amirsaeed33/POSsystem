using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Lookups;

namespace SmartPos.Branches
{
    [Table("AppBranches")]
    public class Branch : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 256;
        public const int MaxCodeLength = 64;
        public const int MaxImagePathLength = 1024;
        public const int MaxAddressLength = 1024;
        public const int MaxEmailLength = 512;
        public const int MaxPhoneLength = 64;
        public const int MaxTaxNumberLength = 128;
        public const int MaxWebsiteLength = 512;
        public const int MaxInvoiceFooterLength = 1024;

        public virtual int? TenantId { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [Required]
        [StringLength(MaxCodeLength)]
        public virtual string Code { get; set; }

        /// <summary>
        /// FK to <see cref="LookUp"/> where Type = BranchStatus (host-scoped).
        /// </summary>
        public virtual int StatusId { get; set; }

        [ForeignKey(nameof(StatusId))]
        public virtual LookUp StatusLookUp { get; set; }

        public virtual bool IsActive { get; set; } = true;

        public virtual bool IsDefault { get; set; }

        [StringLength(MaxImagePathLength)]
        public virtual string ImagePath { get; set; }

        [StringLength(MaxAddressLength)]
        public virtual string InvoiceAddress { get; set; }

        [StringLength(MaxEmailLength)]
        public virtual string InvoiceContactEmail { get; set; }

        [StringLength(MaxPhoneLength)]
        public virtual string InvoiceContactPhone { get; set; }

        [StringLength(MaxTaxNumberLength)]
        public virtual string TaxNumber { get; set; }

        [StringLength(MaxWebsiteLength)]
        public virtual string Website { get; set; }

        [StringLength(MaxInvoiceFooterLength)]
        public virtual string InvoiceFooter { get; set; }

        /// <summary>Default tax % applied on sales for this location.</summary>
        public virtual decimal TaxPercent { get; set; }

        /// <summary>Default discount % applied on sales for this location.</summary>
        public virtual decimal DiscountPercent { get; set; }

        /// <summary>Default discount amount applied on sales for this location.</summary>
        public virtual decimal DiscountAmount { get; set; }
    }
}
