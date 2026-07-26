using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.CompanyProfiles
{
    [Table("AppCompanyProfiles")]
    public class CompanyProfile : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;
        public const int MaxPhoneLength = 32;
        public const int MaxEmailLength = 256;
        public const int MaxAddressLength = 512;
        public const int MaxTaxNumberLength = 64;
        public const int MaxWebsiteLength = 256;
        public const int MaxInvoiceFooterLength = 512;
        public const int MaxImagePathLength = 512;

        public virtual int? TenantId { get; set; }

        /// <summary>
        /// Software / company display name.
        /// </summary>
        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

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
    }
}
