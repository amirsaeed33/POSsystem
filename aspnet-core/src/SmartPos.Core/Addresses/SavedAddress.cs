using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Orders;

namespace SmartPos.Addresses
{
    /// <summary>
    /// Shared saved address for customers, staff, and suppliers (with optional delivery zone).
    /// </summary>
    [Table("AppSavedAddresses")]
    public class SavedAddress : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxLabelLength = 64;
        public const int MaxAddressLength = 512;
        public const int MaxLandmarkLength = 256;
        public const int MaxDeliveryAreaNameLength = 128;

        public virtual int? TenantId { get; set; }

        /// <summary><see cref="SavedAddressOwnerTypes"/> value.</summary>
        public virtual int OwnerType { get; set; }

        /// <summary>AppCustomers / AppStaff / AppSuppliers id for the owner type.</summary>
        public virtual int OwnerId { get; set; }

        /// <summary>Optional short label (Home, Office, etc.).</summary>
        [StringLength(MaxLabelLength)]
        public virtual string Label { get; set; }

        [Required]
        [StringLength(MaxAddressLength)]
        public virtual string Address { get; set; }

        [StringLength(MaxLandmarkLength)]
        public virtual string Landmark { get; set; }

        public virtual int? DeliveryAreaId { get; set; }

        [ForeignKey(nameof(DeliveryAreaId))]
        public virtual DeliveryArea DeliveryArea { get; set; }

        [StringLength(MaxDeliveryAreaNameLength)]
        public virtual string DeliveryAreaName { get; set; }

        public virtual double? Latitude { get; set; }

        public virtual double? Longitude { get; set; }

        public virtual bool IsDefault { get; set; }
    }
}
