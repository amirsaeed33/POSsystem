using Abp.Application.Services.Dto;

namespace SmartPos.Developers.Dto
{
    public class PagedDeveloperResultRequestDto : PagedAndSortedResultRequestDto
    {
        public string Keyword { get; set; }

        public bool? IsActive { get; set; }
    }
}
