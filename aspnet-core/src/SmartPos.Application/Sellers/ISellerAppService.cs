using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using SmartPos.Sellers.Dto;

namespace SmartPos.Sellers
{
    public interface ISellerAppService : IApplicationService
    {
        [AbpAllowAnonymous]
        Task<SellerDto> RegisterAsync(RegisterSellerDto input);

        [AbpAllowAnonymous]
        Task<List<PublicSellerDto>> GetPublicSellersAsync();

        Task<PagedResultDto<SellerDto>> GetAllAsync(PagedSellerResultRequestDto input);

        Task<SellerDto> GetAsync(EntityDto<int> input);

        Task ApproveAsync(EntityDto<int> input);

        Task RejectAsync(RejectSellerDto input);

        Task DeleteAsync(EntityDto<int> input);
    }
}
