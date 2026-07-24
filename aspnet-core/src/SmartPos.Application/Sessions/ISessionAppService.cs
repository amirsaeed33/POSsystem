using System.Threading.Tasks;
using Abp.Application.Services;
using SmartPos.Sessions.Dto;

namespace SmartPos.Sessions
{
    public interface ISessionAppService : IApplicationService
    {
        Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
    }
}
