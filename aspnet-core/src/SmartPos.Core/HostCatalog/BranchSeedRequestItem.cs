using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.HostCatalog
{
    [Table("AppBranchSeedRequestItems")]
    public class BranchSeedRequestItem : CreationAuditedEntity
    {
        public virtual int BranchSeedRequestId { get; set; }

        [ForeignKey(nameof(BranchSeedRequestId))]
        public virtual BranchSeedRequest BranchSeedRequest { get; set; }

        /// <summary>Host catalog Category / Unit / Brand row id.</summary>
        public virtual int HostItemId { get; set; }

        [ForeignKey(nameof(HostItemId))]
        public virtual HostCatalogItem HostItem { get; set; }
    }
}
