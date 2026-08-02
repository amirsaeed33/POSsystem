using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Branches.Dto;

namespace SmartPos.Branches
{
    public interface IBranchAppService : IAsyncCrudAppService<BranchDto, int, PagedBranchResultRequestDto, CreateBranchDto, BranchDto>
    {
        Task<ListResultDto<BranchDto>> GetLookupAsync();
    }
}
