using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Branches.Dto;

namespace SmartPos.Branches
{
    public interface IBranchAppService : IAsyncCrudAppService<BranchDto, int, PagedBranchResultRequestDto, CreateBranchDto, BranchDto>
    {
        Task<ListResultDto<BranchDto>> GetLookupAsync();

        Task<BranchDto> GetInvoiceInfoAsync();

        /// <summary>Host-only: pending tenant branches awaiting approval.</summary>
        Task<ListResultDto<BranchDto>> GetPendingApprovalsAsync();

        /// <summary>Host-only: set status to Approved / Rejected (LookUp Name).</summary>
        Task<BranchDto> ChangeStatusAsync(ChangeBranchStatusDto input);
    }
}
