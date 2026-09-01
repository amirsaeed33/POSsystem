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
                .ForMember(d => d.ProfitPerUnit, opt => opt.MapFrom(s => ProductPricing.ProfitPerUnit(s.Price, s.CostPrice)))
                .ForMember(d => d.ProfitMarginPercent, opt => opt.MapFrom(s => ProductPricing.ProfitMarginPercent(s.Price, s.CostPrice)))
                .ForMember(d => d.StockProfit, opt => opt.Ignore())
                .ForMember(d => d.ImageBase64, opt => opt.Ignore())
                .ForMember(d => d.BranchIds, opt => opt.Ignore())
                .ForMember(d => d.IsShared, opt => opt.Ignore());

            CreateMap<CreateProductDto, Product>()
                .ForMember(d => d.Category, opt => opt.Ignore())
                .ForMember(d => d.Brand, opt => opt.Ignore())
                .ForMember(d => d.Unit, opt => opt.Ignore())
                .ForMember(d => d.ImagePath, opt => opt.Ignore());

            CreateMap<ProductDto, Product>()
                .ForMember(d => d.Category, opt => opt.Ignore())
                .ForMember(d => d.Brand, opt => opt.Ignore())
                .ForMember(d => d.Unit, opt => opt.Ignore())
                .ForMember(d => d.ImagePath, opt => opt.Ignore());
        }
    }
}
