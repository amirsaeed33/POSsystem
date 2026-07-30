using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Emailing
{
    [Table("AppEmailTemplates")]
    public class EmailTemplate : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;
        public const int MaxCodeLength = 64;
        public const int MaxSubjectLength = 256;
        public const int MaxDescriptionLength = 512;

        public virtual int? TenantId { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        /// <summary>
        /// Stable key used by the app (e.g. EmailLoginCode). Unique per tenant.
        /// </summary>
        [Required]
        [StringLength(MaxCodeLength)]
        public virtual string Code { get; set; }

        [Required]
        [StringLength(MaxSubjectLength)]
        public virtual string Subject { get; set; }

        /// <summary>
        /// HTML body. Placeholders use {{Name}} syntax.
        /// </summary>
        [Required]
        public virtual string BodyHtml { get; set; }

        [StringLength(MaxDescriptionLength)]
        public virtual string Description { get; set; }

        public virtual bool IsActive { get; set; } = true;
    }
}
