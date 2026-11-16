using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using SmartPos.Addresses;
using SmartPos.Customers;

namespace SmartPos.Customers.Dto
{
    /// <summary>
    /// Saved address DTO. For customer rows, <see cref="CustomerId"/> mirrors <see cref="OwnerId"/>.
    /// </summary>
    public class CustomerSavedAddressDto : EntityDto
    {
        public int OwnerType { get; set; } = SavedAddressOwnerTypes.Customer;

        public int OwnerId { get; set; }

        /// <summary>Convenience for customer flows (same as OwnerId when OwnerType is Customer).</summary>
        public int CustomerId { get; set; }

        public string Label { get; set; }

        public string Address { get; set; }

        public string Landmark { get; set; }

        public int? DeliveryAreaId { get; set; }

        public string DeliveryAreaName { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public bool IsDefault { get; set; }
    }

    public class SaveCustomerAddressInput
    {
        /// <summary>Staff path: save against a known customer id.</summary>
        public int? CustomerId { get; set; }

        /// <summary>Purchaser / customer online path: resolve customer by phone + branch.</summary>
        public int? BranchId { get; set; }

        [StringLength(Customer.MaxPhoneLength)]
        public string Phone { get; set; }

        [StringLength(SavedAddress.MaxLabelLength)]
        public string Label { get; set; }

        [Required]
        [StringLength(SavedAddress.MaxAddressLength)]
        public string Address { get; set; }

        [StringLength(SavedAddress.MaxLandmarkLength)]
        public string Landmark { get; set; }

        public int? DeliveryAreaId { get; set; }

        [StringLength(SavedAddress.MaxDeliveryAreaNameLength)]
        public string DeliveryAreaName { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public bool IsDefault { get; set; }

        /// <summary>When set, update this row instead of inserting / matching.</summary>
        public int? Id { get; set; }
    }

    public class GetCustomerSavedAddressesInput
    {
        public int? CustomerId { get; set; }

        public int? BranchId { get; set; }

        [StringLength(Customer.MaxPhoneLength)]
        public string Phone { get; set; }
    }

    public class DeleteCustomerSavedAddressInput : EntityDto
    {
        /// <summary>Optional phone for anonymous owner delete.</summary>
        [StringLength(Customer.MaxPhoneLength)]
        public string Phone { get; set; }

        public int? BranchId { get; set; }
    }
}
