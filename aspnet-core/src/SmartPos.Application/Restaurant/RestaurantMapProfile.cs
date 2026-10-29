using AutoMapper;
using SmartPos.Restaurant.Dto;

namespace SmartPos.Restaurant
{
    public class RestaurantMapProfile : Profile
    {
        public RestaurantMapProfile()
        {
            CreateMap<RestaurantTable, RestaurantTableDto>()
                .ForMember(d => d.CurrentVehicleNumber, opt => opt.Ignore());
            CreateMap<RestaurantTableDto, RestaurantTable>()
                .ForMember(x => x.Branch, opt => opt.Ignore())
                .ForMember(x => x.TenantId, opt => opt.Ignore());
            CreateMap<CreateRestaurantTableDto, RestaurantTable>()
                .ForMember(x => x.Branch, opt => opt.Ignore())
                .ForMember(x => x.TenantId, opt => opt.Ignore())
                .ForMember(x => x.Status, opt => opt.Ignore())
                .ForMember(x => x.CurrentOrderId, opt => opt.Ignore());

            CreateMap<RestaurantOrder, RestaurantOrderDto>()
                .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer != null ? s.Customer.Name : string.Empty));
            CreateMap<RestaurantOrderLine, RestaurantOrderLineDto>();

            CreateMap<ProductModifierGroup, ProductModifierGroupDto>();
            CreateMap<ProductModifierOption, ProductModifierOptionDto>();
        }
    }
}
