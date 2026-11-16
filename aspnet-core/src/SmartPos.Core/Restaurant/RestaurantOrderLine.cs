using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Products;

namespace SmartPos.Restaurant
{
    [Table("AppRestaurantOrderLines")]
    public class RestaurantOrderLine : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxProductNameLength = 256;
        public const int MaxKitchenNotesLength = 512;
        public const int MaxModifiersLength = 512;

        public virtual int? TenantId { get; set; }

        public virtual int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual RestaurantOrder Order { get; set; }

        public virtual int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        [StringLength(MaxProductNameLength)]
        public virtual string ProductName { get; set; }

        public virtual decimal Quantity { get; set; } = 1;

        public virtual decimal UnitPrice { get; set; }

        public virtual decimal LineTotal { get; set; }

        [StringLength(MaxKitchenNotesLength)]
        public virtual string KitchenNotes { get; set; }

        [StringLength(MaxModifiersLength)]
        public virtual string SelectedModifiers { get; set; }

        public virtual bool IsSentToKitchen { get; set; } = false;

        public virtual decimal PrintedQuantity { get; set; } = 0;

        public virtual DateTime? SentToKitchenTime { get; set; }
    }
}
