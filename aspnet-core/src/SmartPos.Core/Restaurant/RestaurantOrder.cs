using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;
using SmartPos.Customers;
using SmartPos.Sales;

namespace SmartPos.Restaurant
{
    [Table("AppRestaurantOrders")]
    public class RestaurantOrder : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxOrderNoLength = 64;
        public const int MaxNotesLength = 1024;
        public const int MaxWaiterNameLength = 128;
        public const int MaxTableNoLength = 64;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        [StringLength(MaxOrderNoLength)]
        public virtual string OrderNo { get; set; }

        public virtual int? TableId { get; set; }

        [ForeignKey(nameof(TableId))]
        public virtual RestaurantTable Table { get; set; }

        [StringLength(MaxTableNoLength)]
        public virtual string TableNo { get; set; }

        public virtual OrderType OrderType { get; set; } = OrderType.DineIn;

        public virtual RestaurantOrderStatus Status { get; set; } = RestaurantOrderStatus.Open;

        public virtual int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; }

        [StringLength(MaxWaiterNameLength)]
        public virtual string WaiterName { get; set; }

        [StringLength(MaxNotesLength)]
        public virtual string Notes { get; set; }

        public virtual decimal SubTotal { get; set; }

        public virtual decimal DiscountPercent { get; set; }

        public virtual decimal DiscountAmount { get; set; }

        public virtual decimal TaxPercent { get; set; }

        public virtual decimal TaxAmount { get; set; }

        public virtual decimal ServiceChargePercent { get; set; }

        public virtual decimal ServiceChargeAmount { get; set; }

        public virtual decimal GrandTotal { get; set; }

        public virtual int? SaleId { get; set; }

        [ForeignKey(nameof(SaleId))]
        public virtual Sale Sale { get; set; }

        public virtual DateTime? BilledTime { get; set; }

        public virtual DateTime? SettledTime { get; set; }

        [StringLength(MaxWaiterNameLength)]
        public virtual string LastPrintedBy { get; set; }

        public virtual DateTime? LastPrintedTime { get; set; }

        public virtual ICollection<RestaurantOrderLine> Lines { get; set; } = new List<RestaurantOrderLine>();
    }
}
