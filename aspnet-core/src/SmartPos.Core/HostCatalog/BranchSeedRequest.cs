using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;

namespace SmartPos.HostCatalog
{
    [Table("AppBranchSeedRequests")]
    public class BranchSeedRequest : FullAuditedEntity, IMustHaveTenant
    {
        public const int MaxStatusLength = 32;

        public virtual int TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        public virtual long RequestedByUserId { get; set; }

        /// <summary>Host catalog CompanyType row id.</summary>
        public virtual int CompanyTypeId { get; set; }

        [ForeignKey(nameof(CompanyTypeId))]
        public virtual HostCatalogItem CompanyType { get; set; }

        [Required]
        [StringLength(MaxStatusLength)]
        public virtual string Status { get; set; } = BranchSeedRequestStatuses.Pending;

        public virtual long? ApprovedByUserId { get; set; }

        public virtual DateTime? ApprovedDate { get; set; }

        public virtual ICollection<BranchSeedRequestItem> Items { get; set; }
    }
}
