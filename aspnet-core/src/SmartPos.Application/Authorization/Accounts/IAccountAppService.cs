using System.Threading.Tasks;
using Abp.Application.Services;
using SmartPos.Authorization.Accounts.Dto;

namespace SmartPos.Authorization.Accounts
{
    public interface IAccountAppService : IApplicationService
    {
        Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input);

        Task<RegisterOutput> Register(RegisterInput input);

        /// <summary>
        /// Public self-service signup: creates a new tenant and its admin user credentials.
        /// </summary>
        Task<SignUpTenantOutput> SignUpTenant(SignUpTenantInput input);
    }
}
