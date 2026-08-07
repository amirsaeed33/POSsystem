using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using SmartPos.Branches;

namespace SmartPos.Branches.Dto
{
    public class BranchDto : EntityDto
    {
        public DateTime CreationTime { get; set; }

        [Required]
        [StringLength(Branch.MaxNameLength)]
        public string Name { get; set; }

        [Required]
        [StringLength(Branch.MaxCodeLength)]
        public string Code { get; set; }

        /// <summary>FK to LookUp (BranchStatus).</summary>
        public int StatusId { get; set; }

        /// <summary>LookUp.Name (Pending / Approved / Rejected).</summary>
        public string Status { get; set; }

        /// <summary>LookUp.DisplayName.</summary>
        public string StatusDisplayName { get; set; }

        public int? TenantId { get; set; }

        public string TenancyName { get; set; }

        public bool IsActive { get; set; }

        public bool IsDefault { get; set; }

        [StringLength(Branch.MaxImagePathLength)]
        public string ImagePath { get; set; }

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
