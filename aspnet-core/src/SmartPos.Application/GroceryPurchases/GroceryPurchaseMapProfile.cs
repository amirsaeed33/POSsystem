using AutoMapper;
using SmartPos.GroceryPurchases.Dto;

namespace SmartPos.GroceryPurchases
{
    public class GroceryPurchaseMapProfile : Profile
    {
        public GroceryPurchaseMapProfile()
        {
            CreateMap<GroceryPurchase, GroceryPurchaseDto>()
                .ForMember(d => d.BranchName, opt => opt.MapFrom(s => s.Branch != null ? s.Branch.Name : null))
                .ForMember(d => d.CreatedByName, opt => opt.Ignore());

            CreateMap<CreateGroceryPurchaseDto, GroceryPurchase>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.TenantId, opt => opt.Ignore())
                .ForMember(d => d.Branch, opt => opt.Ignore())
                .ForMember(d => d.UnitRef, opt => opt.Ignore());

            CreateMap<UpdateGroceryPurchaseDto, GroceryPurchase>()
                .ForMember(d => d.TenantId, opt => opt.Ignore())
                .ForMember(d => d.BranchId, opt => opt.Ignore())
                .ForMember(d => d.Branch, opt => opt.Ignore())
                .ForMember(d => d.UnitRef, opt => opt.Ignore());
        }
    }
}
