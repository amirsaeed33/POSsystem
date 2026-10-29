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

        /// <summary>
        /// When true, this slot is a permanent car bay (Car 1, Car 2, …) used like a table.
        /// </summary>
        public virtual bool IsCarBay { get; set; }

        public virtual TableStatus Status { get; set; } = TableStatus.Available;

        public virtual int? CurrentOrderId { get; set; }

        public virtual int? WaiterId { get; set; }

        [StringLength(128)]
        public virtual string WaiterName { get; set; }

        public virtual bool IsActive { get; set; } = true;

        public static bool LooksLikeCarBay(string tableNo, string area, bool isCarBay)
        {
            if (isCarBay)
            {
                return true;
            }

            var areaNorm = (area ?? string.Empty).Trim().ToLowerInvariant();
            if (areaNorm == "car parking" || areaNorm.Contains("car park"))
            {
                return true;
            }

            var no = (tableNo ?? string.Empty).Trim();
            return no.StartsWith("Car ", System.StringComparison.OrdinalIgnoreCase)
                   || no.StartsWith("Car-", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
