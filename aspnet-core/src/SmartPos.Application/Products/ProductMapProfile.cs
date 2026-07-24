using AutoMapper;
using SmartPos.Products.Dto;

namespace SmartPos.Products
{
    public class ProductMapProfile : Profile
    {
        public ProductMapProfile()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : null))
                .ForMember(d => d.BrandName, opt => opt.MapFrom(s => s.Brand != null ? s.Brand.Name : null))
                .ForMember(d => d.UnitName, opt => opt.MapFrom(s => s.Unit != null ? s.Unit.Name : null))
                .ForMember(d => d.ImageBase64, opt => opt.Ignore());

            CreateMap<ProductDto, Product>()
                .ForMember(d => d.Category, opt => opt.Ignore())
                .ForMember(d => d.Brand, opt => opt.Ignore())
                .ForMember(d => d.Unit, opt => opt.Ignore())
                .ForMember(d => d.ImagePath, opt => opt.Ignore())
                .ForMember(d => d.StockQuantity, opt => opt.Ignore());
        }
    }
}
