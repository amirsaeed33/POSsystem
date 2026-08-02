using Abp.Application.Services.Dto;

namespace SmartPos.Branches.Dto
{
    public class PagedBranchResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public bool? IsActive { get; set; }
    }
}
