using Abp.Application.Services.Dto;

namespace SmartPos.Sellers.Dto
{
    public class PublicSellerDto : EntityDto
    {
        public string FullName { get; set; }

        public string City { get; set; }

        public string PhoneNumber { get; set; }

        public string CompanyName { get; set; }

        public decimal CommissionPerSalePkr { get; set; }
    }
}
