using Abp.Application.Services;
using SmartPos.StaffAttendances.Dto;

namespace SmartPos.StaffAttendances
{
    public interface IStaffAttendanceAppService : IAsyncCrudAppService<
        StaffAttendanceDto,
        int,
        PagedStaffAttendanceResultRequestDto,
        CreateStaffAttendanceDto,
        StaffAttendanceDto>
    {
    }
}
