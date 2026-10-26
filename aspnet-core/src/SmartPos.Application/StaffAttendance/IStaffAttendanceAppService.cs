using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
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
        Task<ListResultDto<DailyAttendanceSheetItemDto>> GetDailySheetAsync(
            GetDailyAttendanceSheetInput input);
    }
}
