using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Emailing;

namespace SmartPos.Emailing.Dto
{
    [AutoMapTo(typeof(EmailTemplate))]
    public class CreateEmailTemplateDto
    {
        [Required]
        [StringLength(EmailTemplate.MaxNameLength)]
        public string Name { get; set; }

        [Required]
        [StringLength(EmailTemplate.MaxCodeLength)]
        public string Code { get; set; }

        [Required]
        [StringLength(EmailTemplate.MaxSubjectLength)]
        public string Subject { get; set; }

        [Required]
        public string BodyHtml { get; set; }

        [StringLength(EmailTemplate.MaxDescriptionLength)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
