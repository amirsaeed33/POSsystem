using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;

namespace SmartPos.Sellers.Dto
{
    [AutoMapFrom(typeof(Seller))]
    [AutoMapTo(typeof(Seller))]
    public class SellerDto : EntityDto
    {
        [Required]
        [StringLength(Seller.MaxFullNameLength)]
        public string FullName { get; set; }

        [StringLength(Seller.MaxEmailLength)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(Seller.MaxPhoneLength)]
        public string PhoneNumber { get; set; }

        [StringLength(Seller.MaxCityLength)]
        public string City { get; set; }

        [StringLength(Seller.MaxCnicLength)]
        public string CNIC { get; set; }

        [StringLength(Seller.MaxCompanyNameLength)]
        public string CompanyName { get; set; }

        [StringLength(Seller.MaxExperienceNotesLength)]
        public string ExperienceNotes { get; set; }

        [StringLength(Seller.MaxMessageLength)]
        public string Message { get; set; }

        public SellerStatus Status { get; set; }

        public string StatusName { get; set; }

        public bool IsPubliclyListed { get; set; }

        public decimal CommissionPerSalePkr { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime? ReviewedTime { get; set; }

        [StringLength(Seller.MaxReviewNotesLength)]
        public string ReviewNotes { get; set; }

        [StringLength(Seller.MaxRejectionReasonLength)]
        public string RejectionReason { get; set; }
    }
}
