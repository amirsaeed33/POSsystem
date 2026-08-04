using Abp.Application.Services;
using SmartPos.StaffPayrolls.Dto;

namespace SmartPos.StaffPayrolls
{
    public interface IStaffPayrollAppService : IAsyncCrudAppService<
        StaffPayrollDto,
        int,
        PagedStaffPayrollResultRequestDto,
        CreateStaffPayrollDto,
        StaffPayrollDto>
    {
    }
}
