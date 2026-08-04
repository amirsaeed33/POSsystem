using Abp.Application.Services.Dto;

namespace SmartPos.Staffs.Dto
{
    public class PagedStaffResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public bool? IsActive { get; set; }

        public int? BranchId { get; set; }
    }
}
