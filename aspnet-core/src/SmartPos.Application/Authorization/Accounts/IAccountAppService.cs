using System.Threading.Tasks;
using Abp.Application.Services;
using SmartPos.Authorization.Accounts.Dto;

namespace SmartPos.Authorization.Accounts
{
    public interface IAccountAppService : IApplicationService
    {
        Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input);

        Task<RegisterOutput> Register(RegisterInput input);
    }
}
