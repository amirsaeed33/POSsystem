using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.HostCatalog
{
    /// <summary>
    /// Host-only master catalog: CompanyType rows and their Category / Unit / Brand children.
    /// </summary>
    [Table("AppHostCatalogItems")]
    public class HostCatalogItem : FullAuditedEntity
    {
        public const int MaxTypeLength = 32;
        public const int MaxNameLength = 128;
        public const int MaxSymbolLength = 32;

        [Required]
        [StringLength(MaxTypeLength)]
        public virtual string Type { get; set; }

        /// <summary>
        /// Null when <see cref="Type"/> is CompanyType; otherwise points at the parent CompanyType row.
        /// </summary>
        public virtual int? CompanyTypeId { get; set; }

        [ForeignKey(nameof(CompanyTypeId))]
        public virtual HostCatalogItem CompanyType { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [StringLength(MaxSymbolLength)]
        public virtual string Symbol { get; set; }

        public virtual bool IsActive { get; set; } = true;
    }
}
