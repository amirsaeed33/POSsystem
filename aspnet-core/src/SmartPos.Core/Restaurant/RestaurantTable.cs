using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;

namespace SmartPos.Restaurant
{
    [Table("AppRestaurantTables")]
    public class RestaurantTable : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxTableNoLength = 64;
        public const int MaxAreaLength = 64;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        [Required]
        [StringLength(MaxTableNoLength)]
        public virtual string TableNo { get; set; }

        [StringLength(MaxAreaLength)]
        public virtual string Area { get; set; } = "Main Hall";

        public virtual int Capacity { get; set; } = 4;

        public virtual TableStatus Status { get; set; } = TableStatus.Available;

        public virtual int? CurrentOrderId { get; set; }

        public virtual bool IsActive { get; set; } = true;
    }
}
