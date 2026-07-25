using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Customers;

namespace SmartPos.Customers.Dto
{
    [AutoMapTo(typeof(Customer))]
    public class CreateCustomerDto
    {
        [Required]
        [StringLength(Customer.MaxNameLength)]
        public string Name { get; set; }

        public int CustomerType { get; set; } = CustomerTypes.Direct;

        [StringLength(Customer.MaxPhoneLength)]
        public string Phone { get; set; }

        [StringLength(Customer.MaxEmailLength)]
        public string Email { get; set; }

        [StringLength(Customer.MaxAddressLength)]
        public string Address { get; set; }

        [StringLength(Customer.MaxDescriptionLength)]
        public string Description { get; set; }
    }
}
