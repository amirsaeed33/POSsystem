using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SmartPos.Suppliers;

namespace SmartPos.Suppliers.Dto
{
    [AutoMapFrom(typeof(Supplier))]
    [AutoMapTo(typeof(Supplier))]
    public class SupplierDto : EntityDto
    {
        public int BranchId { get; set; }

        [Required]
        [StringLength(Supplier.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Supplier.MaxPhoneLength)]
        public string Phone { get; set; }

        [StringLength(Supplier.MaxEmailLength)]
        public string Email { get; set; }

        [StringLength(Supplier.MaxAddressLength)]
        public string Address { get; set; }

        [StringLength(Supplier.MaxDescriptionLength)]
        public string Description { get; set; }

        public int? AccountId { get; set; }

        public decimal Balance { get; set; }
    }
}
