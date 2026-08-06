using Abp.Application.Services.Dto;

namespace SmartPos.Branches.Dto
{
    public class PagedBranchResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        /// <summary>Optional filter by BranchStatus LookUp Id.</summary>
        public int? StatusId { get; set; }
    }
}
