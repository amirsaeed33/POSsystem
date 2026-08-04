using Abp.Application.Services;
using SmartPos.Staffs.Dto;

namespace SmartPos.Staffs
{
    public interface IStaffAppService : IAsyncCrudAppService<StaffDto, int, PagedStaffResultRequestDto, CreateStaffDto, StaffDto>
    {
    }
}
