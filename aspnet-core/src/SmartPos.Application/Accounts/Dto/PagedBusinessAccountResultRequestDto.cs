using Abp.Application.Services.Dto;

namespace SmartPos.Accounts.Dto
{
    public class PagedBusinessAccountResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public bool? IsActive { get; set; }
    }
}
