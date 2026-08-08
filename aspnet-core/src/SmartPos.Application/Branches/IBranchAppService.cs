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

        /// <summary>Host-only: set status to Rejected / Pending, or request activation email when Approved is selected.</summary>
        Task<BranchDto> ChangeStatusAsync(ChangeBranchStatusDto input);

        /// <summary>Host-only: email activation link; status stays Pending until the link is opened.</summary>
        Task<BranchDto> RequestBranchActivationAsync(EntityDto<int> input);

        /// <summary>Public: activate a branch via emailed one-time token.</summary>
        Task<ActivateBranchResultDto> ActivateBranchAsync(ActivateBranchInput input);
    }
}
