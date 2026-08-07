using System.ComponentModel.DataAnnotations;
using SmartPos.Branches;

namespace SmartPos.Branches.Dto
{
    public class CreateBranchDto
    {
        [Required]
        [StringLength(Branch.MaxNameLength)]
        public string Name { get; set; }

        [Required]
        [StringLength(Branch.MaxCodeLength)]
        public string Code { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; }

        public string ImageBase64 { get; set; }

        [StringLength(Branch.MaxAddressLength)]
        public string InvoiceAddress { get; set; }

        [EmailAddress]
        [StringLength(Branch.MaxEmailLength)]
        public string InvoiceContactEmail { get; set; }

        [StringLength(Branch.MaxPhoneLength)]
        public string InvoiceContactPhone { get; set; }

        [StringLength(Branch.MaxTaxNumberLength)]
        public string TaxNumber { get; set; }

        [StringLength(Branch.MaxWebsiteLength)]
        public string Website { get; set; }

        [StringLength(Branch.MaxInvoiceFooterLength)]
        public string InvoiceFooter { get; set; }

        [Range(0, 100)]
        public decimal TaxPercent { get; set; }

        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }
    }
}
