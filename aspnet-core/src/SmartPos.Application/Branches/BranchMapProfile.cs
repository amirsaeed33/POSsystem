using AutoMapper;
using SmartPos.Branches.Dto;

namespace SmartPos.Branches
{
    public class BranchMapProfile : Profile
    {
        public BranchMapProfile()
        {
            CreateMap<Branch, BranchDto>()
                .ForMember(d => d.ImageBase64, opt => opt.Ignore());

            CreateMap<BranchDto, Branch>()
                .ForMember(d => d.ImagePath, opt => opt.Ignore());

            CreateMap<CreateBranchDto, Branch>()
                .ForMember(d => d.ImagePath, opt => opt.Ignore());
        }
    }
}
