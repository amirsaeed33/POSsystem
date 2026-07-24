using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SmartPos.Customers;

namespace SmartPos.Customers.Dto
{
    [AutoMapFrom(typeof(Customer))]
    [AutoMapTo(typeof(Customer))]
    public class CustomerDto : EntityDto
    {
        [Required]
        [StringLength(Customer.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Customer.MaxPhoneLength)]
        public string Phone { get; set; }

        [StringLength(Customer.MaxEmailLength)]
        public string Email { get; set; }

        [StringLength(Customer.MaxAddressLength)]
        public string Address { get; set; }

        [StringLength(Customer.MaxDescriptionLength)]
        public string Description { get; set; }

        public int? AccountId { get; set; }

        public decimal Balance { get; set; }
    }
}
