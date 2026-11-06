using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace SmartPos.Developers.Dto
{
    [AutoMapFrom(typeof(Developer))]
    [AutoMapTo(typeof(Developer))]
    public class DeveloperDto : EntityDto
    {
        [Required]
        [StringLength(Developer.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Developer.MaxEmailLength)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(Developer.MaxPhoneLength)]
        public string PhoneNumber { get; set; }

        [StringLength(Developer.MaxDescriptionLength)]
        public string Description { get; set; }

        [StringLength(Developer.MaxImagePathLength)]
        public string ProfileImagePath { get; set; }

        [StringLength(Developer.MaxImagePathLength)]
        public string BackgroundImagePath { get; set; }

        [StringLength(Developer.MaxEducationLength)]
        public string Education { get; set; }

        [StringLength(Developer.MaxTagsLength)]
        public string Tags { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        /// <summary>New profile image as data URL (not persisted on entity).</summary>
        public string ProfileImageBase64 { get; set; }

        /// <summary>New background image as data URL (not persisted on entity).</summary>
        public string BackgroundImageBase64 { get; set; }

        public bool ClearProfileImage { get; set; }

        public bool ClearBackgroundImage { get; set; }
    }
}
