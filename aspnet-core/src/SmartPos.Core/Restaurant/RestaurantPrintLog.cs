using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Restaurant
{
    [Table("AppRestaurantPrintLogs")]
    public class RestaurantPrintLog : CreationAuditedEntity, IMayHaveTenant
    {
        public const int MaxOrderNoLength = 64;
        public const int MaxReceiptTypeLength = 64;
        public const int MaxNameLength = 128;
        public const int MaxRoleLength = 64;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        public virtual int? OrderId { get; set; }

        [StringLength(MaxOrderNoLength)]
        public virtual string OrderNo { get; set; }

        [StringLength(MaxReceiptTypeLength)]
        public virtual string ReceiptType { get; set; }

        public virtual long PrintedByUserId { get; set; }

        [StringLength(MaxNameLength)]
        public virtual string PrintedByName { get; set; }

        [StringLength(MaxRoleLength)]
        public virtual string PrintedByRole { get; set; }

        public virtual DateTime PrintTime { get; set; }
    }
}
