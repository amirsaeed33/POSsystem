using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Developers
{
    /// <summary>
    /// Host-only developer profile cards (super admin CRUD).
    /// </summary>
    [Table("AppDevelopers")]
    public class Developer : FullAuditedEntity
    {
        public const int MaxNameLength = 128;
        public const int MaxEmailLength = 256;
        public const int MaxPhoneLength = 32;
        public const int MaxDescriptionLength = 2000;
        public const int MaxEducationLength = 2000;
        public const int MaxTagsLength = 512;
        public const int MaxImagePathLength = 512;

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [StringLength(MaxEmailLength)]
        public virtual string Email { get; set; }

        [StringLength(MaxPhoneLength)]
        public virtual string PhoneNumber { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }

        /// <summary>Profile / avatar image relative path under wwwroot.</summary>
        [StringLength(MaxImagePathLength)]
        public virtual string ProfileImagePath { get; set; }

        /// <summary>Card cover / background image relative path under wwwroot.</summary>
        [StringLength(MaxImagePathLength)]
        public virtual string BackgroundImagePath { get; set; }

        /// <summary>Education history (free text / line-separated).</summary>
        [StringLength(MaxEducationLength)]
        public virtual string Education { get; set; }

        /// <summary>Comma-separated skill / role tags.</summary>
        [StringLength(MaxTagsLength)]
        public virtual string Tags { get; set; }

        public virtual bool IsActive { get; set; } = true;

        public virtual int SortOrder { get; set; }
    }
}
