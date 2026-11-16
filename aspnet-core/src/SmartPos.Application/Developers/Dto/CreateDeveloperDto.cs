using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;

namespace SmartPos.Developers.Dto
{
    [AutoMapTo(typeof(Developer))]
    public class CreateDeveloperDto
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

        [StringLength(Developer.MaxEducationLength)]
        public string Education { get; set; }

        [StringLength(Developer.MaxTagsLength)]
        public string Tags { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        public string ProfileImageBase64 { get; set; }

        public string BackgroundImageBase64 { get; set; }
    }
}
