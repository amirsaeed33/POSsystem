using Abp.Application.Services.Dto;

namespace SmartPos.Emailing.Dto
{
    public class PagedEmailTemplateResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public bool? IsActive { get; set; }
    }
}
