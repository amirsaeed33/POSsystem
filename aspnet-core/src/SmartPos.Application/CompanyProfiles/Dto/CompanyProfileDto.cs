using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using SmartPos.CompanyProfiles;

namespace SmartPos.CompanyProfiles.Dto
{
    public class CompanyProfileDto : EntityDto
    {
        [Required]
        [StringLength(CompanyProfile.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(CompanyProfile.MaxImagePathLength)]
        public string ImagePath { get; set; }

        public string ImageBase64 { get; set; }

        [StringLength(CompanyProfile.MaxAddressLength)]
        public string InvoiceAddress { get; set; }

        [EmailAddress]
        [StringLength(CompanyProfile.MaxEmailLength)]
        public string InvoiceContactEmail { get; set; }

        [StringLength(CompanyProfile.MaxPhoneLength)]
        public string InvoiceContactPhone { get; set; }

        [StringLength(CompanyProfile.MaxTaxNumberLength)]
        public string TaxNumber { get; set; }

        [StringLength(CompanyProfile.MaxWebsiteLength)]
        public string Website { get; set; }

        [StringLength(CompanyProfile.MaxInvoiceFooterLength)]
        public string InvoiceFooter { get; set; }
    }
}
