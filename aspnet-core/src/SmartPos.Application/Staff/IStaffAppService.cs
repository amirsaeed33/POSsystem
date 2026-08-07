using System.Threading.Tasks;
using Abp.Application.Services;
using SmartPos.Staffs.Dto;

namespace SmartPos.Staffs
{
    public interface IStaffAppService : IAsyncCrudAppService<StaffDto, int, PagedStaffResultRequestDto, CreateStaffDto, StaffDto>
    {
        Task<StaffDto> CreateLoginAsync(CreateStaffLoginDto input);

        Task ChangeLoginPasswordAsync(ChangeStaffLoginPasswordDto input);
    }
}
