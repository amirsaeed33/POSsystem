using AutoMapper;
using SmartPos.CompanyProfiles.Dto;

namespace SmartPos.CompanyProfiles
{
    public class CompanyProfileMapProfile : Profile
    {
        public CompanyProfileMapProfile()
        {
            CreateMap<CompanyProfile, CompanyProfileDto>()
                .ForMember(d => d.ImageBase64, opt => opt.Ignore());

            CreateMap<CompanyProfileDto, CompanyProfile>()
                .ForMember(d => d.ImagePath, opt => opt.Ignore());

            CreateMap<CreateCompanyProfileDto, CompanyProfile>()
                .ForMember(d => d.ImagePath, opt => opt.Ignore());
        }
    }
}
