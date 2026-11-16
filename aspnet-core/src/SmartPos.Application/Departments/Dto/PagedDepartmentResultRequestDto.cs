using Abp.Application.Services.Dto;

namespace SmartPos.Departments.Dto
{
    public class PagedDepartmentResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }
    }
}

