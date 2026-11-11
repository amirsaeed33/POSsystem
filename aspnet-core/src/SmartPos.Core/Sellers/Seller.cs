using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Sellers
{
    /// <summary>
    /// Software reseller / affiliate applications (host review + public listing when approved).
    /// </summary>
    [Table("AppSellers")]
    public class Seller : FullAuditedEntity
    {
        public const int MaxFullNameLength = 128;
        public const int MaxEmailLength = 256;
        public const int MaxPhoneLength = 32;
        public const int MaxCityLength = 128;
        public const int MaxCnicLength = 32;
        public const int MaxCompanyNameLength = 128;
        public const int MaxExperienceNotesLength = 2000;
        public const int MaxMessageLength = 2000;
        public const int MaxReviewNotesLength = 1000;
        public const int MaxRejectionReasonLength = 1000;
        public const decimal DefaultCommissionPerSalePkr = 3000m;

        [Required]
        [StringLength(MaxFullNameLength)]
        public virtual string FullName { get; set; }

        [StringLength(MaxEmailLength)]
        public virtual string Email { get; set; }

        [Required]
        [StringLength(MaxPhoneLength)]
        public virtual string PhoneNumber { get; set; }

        [StringLength(MaxCityLength)]
        public virtual string City { get; set; }

        [StringLength(MaxCnicLength)]
        public virtual string CNIC { get; set; }

        [StringLength(MaxCompanyNameLength)]
        public virtual string CompanyName { get; set; }

        [StringLength(MaxExperienceNotesLength)]
        public virtual string ExperienceNotes { get; set; }

        [StringLength(MaxMessageLength)]
        public virtual string Message { get; set; }

        public virtual SellerStatus Status { get; set; } = SellerStatus.Pending;

        /// <summary>True only when approved and allowed on the public landing list.</summary>
        public virtual bool IsPubliclyListed { get; set; }

        public virtual decimal CommissionPerSalePkr { get; set; } = DefaultCommissionPerSalePkr;

        public virtual DateTime? ReviewedTime { get; set; }

        [StringLength(MaxReviewNotesLength)]
        public virtual string ReviewNotes { get; set; }

        [StringLength(MaxRejectionReasonLength)]
        public virtual string RejectionReason { get; set; }
    }
}
