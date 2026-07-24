using System.Threading.Tasks;
using Abp.Application.Services;
using SmartPos.Dashboard.Dto;

namespace SmartPos.Dashboard
{
    public interface IDashboardAppService : IApplicationService
    {
        Task<DashboardDto> GetAsync();
    }
}
