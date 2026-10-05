using System.Threading.Tasks;
using Abp.Application.Services;
using SmartPos.BranchNotificationSettings.Dto;

namespace SmartPos.BranchNotificationSettings
{
    public interface IBranchNotificationSettingAppService : IApplicationService
    {
        Task<BranchNotificationSettingDto> GetAsync(int branchId);
        Task<BranchNotificationSettingDto> CreateOrUpdateAsync(CreateOrUpdateBranchNotificationSettingDto input);
        Task DeleteAsync(int branchId);
    }
}
