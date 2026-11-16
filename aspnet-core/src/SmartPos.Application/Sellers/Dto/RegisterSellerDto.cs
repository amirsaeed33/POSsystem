using System.ComponentModel.DataAnnotations;

namespace SmartPos.Sellers.Dto
{
    public class RegisterSellerDto
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
    }
}
