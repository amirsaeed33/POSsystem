using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Suppliers;

namespace SmartPos.Suppliers.Dto
{
    [AutoMapTo(typeof(Supplier))]
    public class CreateSupplierDto
    {
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
    }
}
