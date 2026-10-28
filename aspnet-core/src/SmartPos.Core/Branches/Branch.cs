using System;
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
        public const int MaxActivationTokenHashLength = 128;
        public const int MaxDateFormatLength = 32;
        public const int MaxCurrencyFormatLength = 32;

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

        [StringLength(MaxWebsiteLength)]
        public virtual string FacebookUrl { get; set; }

        [StringLength(MaxWebsiteLength)]
        public virtual string InstagramUrl { get; set; }

        [StringLength(MaxWebsiteLength)]
        public virtual string YoutubeUrl { get; set; }

        [StringLength(MaxDateFormatLength)]
        public virtual string DateFormat { get; set; } = "dd/MM/yyyy";

        [StringLength(MaxCurrencyFormatLength)]
        public virtual string CurrencyFormat { get; set; } = "PKR";

        [StringLength(MaxInvoiceFooterLength)]
        public virtual string InvoiceFooter { get; set; }

        /// <summary>Default tax % applied on sales for this location.</summary>
        public virtual decimal TaxPercent { get; set; }

        /// <summary>Default discount % applied on sales for this location.</summary>
        public virtual decimal DiscountPercent { get; set; }

        /// <summary>Default discount amount applied on sales for this location.</summary>
        public virtual decimal DiscountAmount { get; set; }

        /// <summary>Business type for this location: Retail = 1, Restaurant = 2.</summary>
        public virtual BusinessType BusinessType { get; set; } = BusinessType.Retail;

        /// <summary>Optional default dine-in service charge % applied for restaurant branches.</summary>
        public virtual decimal ServiceChargePercent { get; set; }

        /// <summary>SHA256 (Base64) of the one-time branch activation token emailed after host approval.</summary>
        [StringLength(MaxActivationTokenHashLength)]
        public virtual string ActivationTokenHash { get; set; }

        public virtual DateTime? ActivationTokenExpiresAt { get; set; }

        /// <summary>Optional GPS Latitude of this store / branch location.</summary>
        public virtual double? Latitude { get; set; }

        /// <summary>Optional GPS Longitude of this store / branch location.</summary>
        public virtual double? Longitude { get; set; }

        /// <summary>
        /// Radius in feet for waiter/rider auto attendance check-in on login.
        /// Staff within this distance of the branch pin are marked Present automatically.
        /// (DB column name remains AttendanceAutoCheckInRadiusGaz for compatibility.)
        /// </summary>
        [Column("AttendanceAutoCheckInRadiusGaz")]
        public virtual int AttendanceAutoCheckInRadiusFeet { get; set; } = DefaultAttendanceAutoCheckInRadiusFeet;

        public const int DefaultAttendanceAutoCheckInRadiusFeet = 200;

        /// <summary>1 foot = 0.3048 meters.</summary>
        public const double FeetToMeters = 0.3048;
    }
}
