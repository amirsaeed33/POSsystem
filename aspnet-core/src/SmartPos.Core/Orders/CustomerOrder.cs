using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;
using SmartPos.Customers;
using SmartPos.Sales;

namespace SmartPos.Orders
{
    [Table("AppCustomerOrders")]
    public class CustomerOrder : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxOrderNoLength = 64;
        public const int MaxNotesLength = 512;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        [Required]
        public virtual int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; }

        public virtual DateTime OrderDate { get; set; }

        [StringLength(MaxOrderNoLength)]
        public virtual string OrderNo { get; set; }

        public virtual CustomerOrderStatus Status { get; set; }

        public virtual decimal SubTotal { get; set; }

        public virtual decimal DeliveryFee { get; set; }

        public virtual int? DeliveryAreaId { get; set; }

        [ForeignKey(nameof(DeliveryAreaId))]
        public virtual DeliveryArea DeliveryArea { get; set; }

        [StringLength(128)]
        public virtual string DeliveryAreaName { get; set; }

        public virtual bool IsFreeDelivery { get; set; }

        [StringLength(256)]
        public virtual string FreeDeliveryReason { get; set; }

        public virtual int? DeliveryPromotionId { get; set; }

        [ForeignKey(nameof(DeliveryPromotionId))]
        public virtual DeliveryPromotion DeliveryPromotion { get; set; }

        public virtual decimal? OriginalDeliveryFee { get; set; }

        [StringLength(256)]
        public virtual string DeliveryFeeOverrideReason { get; set; }

        public virtual long? DeliveryFeeOverriddenByUserId { get; set; }

        public virtual DateTime? DeliveryFeeOverrideTime { get; set; }

        public virtual decimal TotalAmount { get; set; }

        [StringLength(MaxNotesLength)]
        public virtual string Notes { get; set; }

        public virtual int? SaleId { get; set; }

        [ForeignKey(nameof(SaleId))]
        public virtual Sale Sale { get; set; }

        public virtual ICollection<CustomerOrderLine> Lines { get; set; }
    }
}
